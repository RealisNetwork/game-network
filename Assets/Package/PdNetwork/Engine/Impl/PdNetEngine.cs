using System;
using System.Threading;
using PdNetwork.Client;
using PdNetwork.Client.Impl;
using PdNetwork.Exchange;
using PdNetwork.Exchange.Requests;
using PdNetwork.Exchange.Responses;
using PdNetwork.LagMonitor;
using PdNetwork.LagMonitor.Impl;
using UnityEngine;

namespace PdNetwork.Engine.Impl
{
    public class PdNetEngine : IPdNetEngine
    {
        private byte _appId;
        private string _authToken;
        private string _apiVersion;
        private ISocketClient _socketClient;
        private IPingMonitor _pingMonitor;
        
        private string _sessionToken;
        
        public bool IsConnecting { get; private set; }
        public bool IsConnected => _socketClient.Connected;

        public Action OnConfigured { get; set; }
        public Action OnConnected { get; set; }
        public Action<string> OnConnectError { get; set; }
        public Action OnConnectionResume { set; get; }
        public Action<string> OnDisconnected { set; get; }
        public Action<IResponse> OnResponse { get; set; }
        public Action<byte[]> OnUdpResponse { get; set; }
        public Action<int> OnReconnectionTry { get; set; }

        private string _host;
        private int _port;

        private PdUdpClient _udpClient;
        
        public void Configure(byte appId, string authToken, string apiVersion, ISocketClient socketClient, int reconnectionSeconds)
		{
			_appId = appId;
            _authToken = authToken;
            _apiVersion = apiVersion;
            _socketClient = socketClient;
            _socketClient.Init();
            _socketClient.ReconnectionSeconds = reconnectionSeconds;
            
            _socketClient.OnConnect += OnSocketConnect;
            _socketClient.OnResponse += OnSocketResponse;
            _socketClient.OnDisconnect += OnSocketDisconnect;
            _socketClient.OnReconnectionTry += OnSocketReconnectionTry;
            _socketClient.OnIoError += OnSocketIOError;
            
            _pingMonitor = new DefaultPingMonitor(this, 4, 10);
            
            OnConfigured?.Invoke();
        }

        public void ConfigureUdp(string outHost, int outPort, int inPort)
        {
            _udpClient = new PdUdpClient();
            _udpClient.OnResponse = OnUdpResponse;
            _udpClient.Configure(outHost, outPort, inPort);
        }

        public IPingMonitor PingMonitor
        {
            set => _pingMonitor = value;
        }

        private void OnSocketReconnectionTry(int attempt)
        {
            OnReconnectionTry?.Invoke(attempt);
        }

        public long AverageRoundPing => _pingMonitor.AverageRoundPing;
        
        public void Destroy()
        {
            if (_udpClient != null)
            {
                _udpClient.Destroy();
                _udpClient.OnResponse = null;   
            }

            if(_socketClient == null)
                return;
            _socketClient.OnConnect -= OnSocketConnect;
            _socketClient.OnResponse -= OnSocketResponse;
            _socketClient.OnDisconnect -= OnSocketDisconnect;
            _socketClient.OnReconnectionTry -= OnSocketReconnectionTry;
            _socketClient.OnIoError -= OnSocketIOError;
            _socketClient.Destroy();
        }

        public void Connect(string host, int port)
        {
            _host = host;
            _port = port;
            
            if (IsConnected)
            {
                Debug.LogWarning("Already connected");   
            }
            else if(IsConnecting)
            {
                Debug.LogWarning("A connection attempt is already in progress");
            }
            else
            {
                IsConnecting = true;
                _socketClient.Connect(host, port);
            }

        }

        public void KillConnection()
        {
            _socketClient.KillConnection();
        }

        public void Reconnect()
        {
            Connect(_host, _port);
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                if (_socketClient.ReconnectionSeconds > 0)
                {
                    // this.Send((IRequest) new ManualDisconnectionRequest());
                    Thread.Sleep(100);
                }
            }
            else
            {
                Debug.LogError("You are not connected");   
            }
            HandleClientDisconnection("manual");
        }
        
        private void OnSocketConnect(bool isReconnection)
        {
            OnConnected?.Invoke();
            _pingMonitor.Start();
            SendHandshakeRequest(isReconnection);
        }
        
        private void OnSocketIOError(string error)
        {
            OnConnectError?.Invoke(error);
            IsConnecting = false;
        }
        
        private void OnSocketDisconnect(string reason)
        {
            _pingMonitor.Stop();
            OnDisconnected?.Invoke(reason);
        }

        private void SendHandshakeRequest(bool isReconnection)
        {
            Send(new HandshakeReq(_appId, _authToken, _apiVersion, isReconnection ? _sessionToken : null));
        }

        private void OnSocketResponse(IResponse response)
        {
            if (response is HandshakeResp handshakeResponse)
            {
                HandleHandshake(handshakeResponse);
            } 
            OnResponse?.Invoke(response);
        }
        
        private void HandleHandshake(HandshakeResp handshakeResp)
        {
            if (handshakeResp.Success)
            {
                if (_socketClient.IsReconnecting)
                {
                    _socketClient.IsReconnecting = false;
                    OnConnectionResume?.Invoke();
                }
                else
                {
                    IsConnecting = false;
                    _sessionToken = handshakeResp.SessionToken;
                }
            }
            else
            {
                IsConnecting = false;
            }
        }


        public void Send(IRequest request)
        {
            if (!IsConnected)
            {   
                Debug.LogWarning("You are not connected. Request cannot be sent: " + request);
            }
            else
            {
                _socketClient.Send(request);
            }
        }

        public void SendUdp(IRequest request) => _udpClient?.Send(request);

        public void SendUdp(byte[] bytes) => _udpClient?.Send(bytes); 
        
        private void HandleClientDisconnection(string reason)
        {
           _socketClient.ReconnectionSeconds = 0;
           _socketClient.Disconnect(reason); 
           Reset();
        }

        private void Reset()
        {
            // _sessionToken = null;
            _pingMonitor?.Destroy();
            _pingMonitor = new DefaultPingMonitor(this, 4, 10);
        }
    }
}
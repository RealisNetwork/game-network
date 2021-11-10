using System;
using System.Net.Sockets;
using System.Timers;
using PdNetwork.Exchange;
using PdNetwork.Sockets;
using PdNetwork.Utils;
using PdNetwork.Utils.Pools;
using UnityEngine;

namespace PdNetwork.Client.Impl
{
    public class SocketClient : ISocketClient
    {
        private const double ReconnectionDelayMillis = 4000.0;

        private readonly IExchangeManager _exchangeManager;

        private int _reconnectionSeconds;
        private bool _attemptingReconnection;
        private DateTime _firstReconnectAttempt = DateTime.MinValue;

        private int _reconnectCounter;
        private Timer _retryTimer;

        private ISocketLayer _socket;

        private string _lastHost;
        private int _lastTcpPort;

        public bool Connected => _socket != null && _socket.IsConnected;

        public Action<bool> OnConnect { get; set; }
        public Action<string> OnDisconnect { get; set; }
        public Action<string> OnIoError { get; set; }
        public Action<string> OnDataError { get; set; }
        public Action<IResponse> OnResponse { get; set; }
        public Action<int> OnReconnectionTry { get; set; }

        public SocketClient(ISocketLayer socket, IExchangeManager exchangeManager)
        {
            _socket = socket;
            _exchangeManager = exchangeManager;
        }

        public int ReconnectionSeconds
        {
            get => _reconnectionSeconds;
            set => _reconnectionSeconds = value < 0 ? 0 : value;
        }

        public bool IsReconnecting
        {
            get => _attemptingReconnection;
            set => _attemptingReconnection = value;
        }

        public void Send(IRequest request)
        {
            var bytes = request.GetRequestBytes();
            _socket.Write(bytes);
        }

        public void Init()
        {
            _socket.OnConnect += OnSocketConnect;
            _socket.OnDisconnect += OnSocketClose;
            _socket.OnData += OnSocketData;
            _socket.OnError += OnSocketError;
        }

        public void Destroy()
        {
            _socket.OnConnect -= OnSocketConnect;
            _socket.OnDisconnect -= OnSocketClose;
            _socket.OnData -= OnSocketData;
            _socket.OnError -= OnSocketError;
            if (_socket.IsConnected)
                _socket.Disconnect();
            
            _socket.Destroy();
            _socket = null;
        }

        public void Connect(string host, int port)
        {
            _lastHost = host;
            _lastTcpPort = port;

            _socket.Connect(_lastHost, _lastTcpPort);
        }

        public void Disconnect(string reason = null)
        {
            _socket.Disconnect(reason);
        }

        public void KillConnection()
        {
            _socket.Kill();
            OnSocketClose();
        }

        private void OnSocketConnect()
        {
            _reconnectCounter = 0;
            OnConnect?.Invoke(_attemptingReconnection);
        }

        private void OnSocketData(byte[] data)
        {
            try
            {
                if (data.Length < 3)
                {
                    Debug.LogError("Receive junk");
                    return;
                }

                var reader = ByteReaderPool.Instance.GetReader(data);

                do
                {
                    var length = reader.ReadInt16();
                    var header = reader.ReadByte();
                    var body = reader.ReadBytes(length - 1);
                    var response = _exchangeManager.ReadResponse(header, body);
                    OnResponse?.Invoke(response);
                } while (reader.Position < reader.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError("## SocketDataError: " + ex);
                OnDataError?.Invoke(ex.ToString());
            }
        }

        private void OnSocketClose(string reason = null)
        {
            ExecuteDisconnection(reason);
            return; // TODO fix reconnect
            if ((!_attemptingReconnection && ReconnectionSeconds == 0) | reason == "manual")
            {
                _firstReconnectAttempt = DateTime.MinValue;
                ExecuteDisconnection(reason);
            }
            else if (_attemptingReconnection)
            {
                Reconnect();
            }
            else
            {
                _attemptingReconnection = true;
                _firstReconnectAttempt = DateTime.Now;
                Reconnect();
            }
        }

        private void OnSocketError(string message, SocketError se)
        {
            if (_attemptingReconnection)
            {
                Reconnect();
            }
            else
            {
                OnIoError?.Invoke(message);
            }
        }

        private void Reconnect()
        {
            if (!_attemptingReconnection)
                return;
            
            var timeSpan = _firstReconnectAttempt + new TimeSpan(0, 0, ReconnectionSeconds) - DateTime.Now;
            if (timeSpan > TimeSpan.Zero)
            {
                Debug.Log("Reconnection attempt: " + (_reconnectCounter + 1) + " - time left:" + timeSpan.TotalSeconds +
                          " sec.");
                SetTimeout(OnRetryConnectionEvent, ReconnectionDelayMillis);
                _reconnectCounter++;
                OnReconnectionTry?.Invoke(_reconnectCounter);
            }
            else
            {
                _attemptingReconnection = false;
                ExecuteDisconnection();
            }
        }


        private void SetTimeout(ElapsedEventHandler handler, double timeout)
        {
            if (_retryTimer == null)
            {
                _retryTimer = new Timer(timeout);
                _retryTimer.Elapsed += handler;
            }

            _retryTimer.AutoReset = false;
            _retryTimer.Enabled = true;
            _retryTimer.Start();
        }

        private void OnRetryConnectionEvent(object source, ElapsedEventArgs e)
        {
            _retryTimer.Enabled = false;
            _retryTimer.Stop();
            _socket.Connect(_lastHost, _lastTcpPort);
        }

        private void ExecuteDisconnection(string reason = null)
        {
            OnDisconnect?.Invoke(reason);
        }
    }
}
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

namespace PdNetwork.Sockets.Impl
{
    public class WebSocketLayer : ISocketLayer
    {
        private static int _connId;
        
        private Thread _thrConnect;
        private string _uri;
        private WebSocket _connection;

        private volatile bool _isDisconnecting;

        private ConnectState State { set; get; }

        public bool IsConnected => State == ConnectState.Connected;
        
        public Action<string, SocketError> OnError { get; set; }

        public Action OnConnect { get; set; }

        public Action<string> OnDisconnect { get; set; }
        
        public Action<byte[]> OnData { get; set; }

        public bool RequiresConnection => true;

        private readonly bool _security;

        public WebSocketLayer(bool security)
        {
            _security = security;
        }

        private void LogWarn(string msg)
        {
            Debug.LogWarning("[WebSocketLayer] " + msg);
        }

        private void LogError(string msg, string stacktrace = null)
        {
            var error = stacktrace != null ? msg + ", \n" + stacktrace : msg;
            Debug.LogError("[WebSocketLayer] " + error);
        }
        
        private void ConnectThread()
        {
            Thread.CurrentThread.Name = "ConnectionThread" + _connId++;
            try
            {
                _connection = new WebSocket(_uri);
                if (_security)
                {
                    _connection.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;   
                }
                _connection.OnOpen += ConnectionOnOpen;
                _connection.OnMessage += Read;
                _connection.OnError += ConnectionOnError;
                _connection.Connect();
            }
            catch (Exception ex)
            {
                HandleError("General exception on connection: " + ex.Message + " " + ex.StackTrace);
            }
        }

        private void ConnectionOnOpen(object sender, EventArgs e)
        {
            State = ConnectState.Connected;
            OnConnect?.Invoke();
        }
        
        private void ConnectionOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            HandleError(errorEventArgs.Message);
        }

        private void Read(object sender, MessageEventArgs args)
        {
            try
            {
                if (State != ConnectState.Connected)
                    return;
                    
                if (!_connection.IsAlive)
                    return;
                    
                if (_connection.ReadyState != WebSocketState.Open)
                    return;

                var byteBuffer = args.RawData;
                if (byteBuffer.Length < 1)
                {
                    HandleError("Connection closed by the remote side");
                    return;
                }
                    
                HandleBinaryData(byteBuffer, byteBuffer.Length);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    HandleError("General error reading data from socket: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private void HandleBinaryData(byte[] buf, int size)
        {
            var data = new byte[size];
            Buffer.BlockCopy(buf, 0, data, 0, size);
            OnData?.Invoke(data);
        }

        public void Connect(string host, int port)
        {
            if ((uint) State > 0U)
            {
                LogWarn("Call to Connect method ignored, as the socket is already connected");
            }
            else
            {
                _uri = host + ":" + port;

                State = ConnectState.Connecting;
                
                _thrConnect = new Thread(ConnectThread);
                _thrConnect.Start();
            }
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        public void Disconnect(string reason)
        {
            if (State != ConnectState.Connected)
            {
                LogWarn("Calling disconnect when the socket is not connected");
            }
            else
            {
                _isDisconnecting = true;
                try
                {
                    _connection.Close();

                    _thrConnect.Abort();
                }
                catch (Exception ex)
                {
                    LogError(ex.Message, ex.StackTrace);
                }

                HandleDisconnection(reason);
                _isDisconnecting = false;
            }
        }

        public void Kill()
        {
            State = ConnectState.Disconnected;
            _connection?.Close();
        }

        public void Destroy()
        {
            _connection.OnOpen -= ConnectionOnOpen;
            _connection.OnMessage -= Read;
            _connection.OnError -= ConnectionOnError;
            if (_connection.IsAlive)
            {
                _connection.Close();
            }
            if (_thrConnect.IsAlive)
            {
                _thrConnect.Abort();   
            }
        }

        public void Write(byte[] data)
        {
            WriteSocket(data);
        }

        private void WriteSocket(byte[] buf)
        {
            if (State != ConnectState.Connected)
                LogError("Trying to write to disconnected socket");
            else
                try
                {
                    _connection.Send(buf);
                }
                catch (SocketException ex)
                {
                    HandleError("Error writing to socket: " + ex.Message, ex.StackTrace, ex.SocketErrorCode);
                }
                catch (Exception ex)
                {
                    HandleError("General error writing to socket: " + ex.Message, ex.StackTrace);
                }
        }

        private void HandleError(string err, string stacktrace = null, SocketError se = SocketError.NotSocket)
        {
            State = ConnectState.Disconnected;

            if (!_isDisconnecting)
            {
                LogError(err, stacktrace);
                OnError(err, se);
            }
            
            HandleDisconnection();
        }

        private void HandleDisconnection(string reason = null)
        {
            if (State == ConnectState.Disconnected)
                return;

            _connection.OnOpen -= ConnectionOnOpen;
            State = ConnectState.Disconnected;
            OnDisconnect?.Invoke(reason);
        }
    }
    
}
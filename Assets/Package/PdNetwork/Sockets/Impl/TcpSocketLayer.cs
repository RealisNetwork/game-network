using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace PdNetwork.Sockets.Impl
{
    public class TcpSocketLayer : ISocketLayer
    {
        private const int ReadBufferSize = 4096;
        private static int _connId;
        
        private Thread _thrConnect;
        private string _host;
        private int _socketNumber;
        private TcpClient _connection;
        private NetworkStream _networkStream;
        private Thread _thrSocketReader;
        private readonly byte[] _byteBuffer = new byte[ReadBufferSize];

        private volatile bool _isDisconnecting;

        private ConnectState State { set; get; }

        public TcpSocketLayer()
        {
            State = ConnectState.Disconnected;
        }

        public bool IsConnected => State == ConnectState.Connected;
        
        public Action<string, SocketError> OnError { get; set; }

        public Action OnConnect { get; set; }

        public Action<string> OnDisconnect { get; set; }
        
        public Action<byte[]> OnData { get; set; }

        public bool RequiresConnection => true;

        private void LogWarn(string msg)
        {
            Debug.LogWarning("[TcpSocketLayer] " + msg);
        }

        private void LogError(string msg, string stacktrace = null)
        {
            var error = stacktrace != null ? msg + ", \n" + stacktrace : msg;
            Debug.LogError("[TcpSocketLayer] " + error);
        }

        private void ConnectThread()
        {
            Thread.CurrentThread.Name = "ConnectionThread" + _connId++;
            try
            {
                _connection = new TcpClient(_host, _socketNumber)
                {
                    NoDelay = true
                };
                _networkStream = _connection.GetStream();

                State = ConnectState.Connected;
                OnConnect?.Invoke();
                
                _thrSocketReader = new Thread(Read)
                {
                    IsBackground = true
                };
                _thrSocketReader.Start();
            }
            catch (SocketException ex)
            {
                HandleError("Connection error: " + ex.Message, ex.StackTrace, ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                HandleError("General exception on connection: " + ex.Message + " " + ex.StackTrace);
            }
        }

        private void Read()
        {
            while (true)
            {
                try
                {
                    if (State != ConnectState.Connected)
                        break;
                    
                    if (!_connection.Client.Connected)
                        continue;
                    
                    if (!_networkStream.CanRead)
                        continue;
                    
                    var size = _networkStream.Read(_byteBuffer, 0, ReadBufferSize);
                    if (size < 1)
                    {
                        HandleError("Connection closed by the remote side");
                        break;
                    }
                    
                    HandleBinaryData(_byteBuffer, size);
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        HandleError("General error reading data from socket: " + ex.Message, ex.StackTrace);
                    }

                    break;
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
                _host = host;
                _socketNumber = port;

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
                    _connection.Client.Shutdown(SocketShutdown.Both);
                    _connection.Close();
                    _networkStream.Close();
                    
                    _thrConnect.Abort();
                    _thrSocketReader.Abort();
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
                    _networkStream.Write(buf, 0, buf.Length);
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

            State = ConnectState.Disconnected;
            OnDisconnect?.Invoke(reason);
        }
    }
}
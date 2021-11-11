using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PdNetwork.Utils;
using UnityEngine;

namespace PdNetwork.Sockets.Impl
{
    public class UdpSocketLayer
    {
        private const int ReceivePollingTime = 500000; //0.5 second
        
        private static int _connId;
        
        private Thread _thrConnect;
        private IPEndPoint _sendEndPoint;
        private int _inPort;
        private Socket _socket;
        private Thread _thrSocketReader;
        
        public Action<string, SocketError> OnError { get; set; }

        
        public Action<byte[]> OnData { get; set; }

        private bool _active;

        private void LogWarn(string msg)
        {
            Debug.LogWarning("[UdpSocketLayer] " + msg);
        }

        private void LogError(string msg, string stacktrace = null)
        {
            var error = stacktrace != null ? msg + ", \n" + stacktrace : msg;
            Debug.LogError("[UdpSocketLayer] " + error);
        }

        private void InitThread()
        {
            if(_active)
                return;
            
            Thread.CurrentThread.Name = "ConnectionThread" + _connId++;
            try
            {
                
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.ReceiveTimeout = 500;
                _socket.SendTimeout = 500;
                _socket.ReceiveBufferSize = 1024 * 1024;
                _socket.SendBufferSize = 1024 * 1024;
                
                _socket.ExclusiveAddressUse = true;
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                
                _socket.DontFragment = true;
                _socket.EnableBroadcast = true;

                var ep = new IPEndPoint(IPAddress.Any, _inPort);
                _socket.Bind(ep);
                
                var localPort  = ((IPEndPoint) _socket.LocalEndPoint).Port;

                _active = true;
                
                _thrSocketReader = new Thread(Read)
                {
                    Name = "SocketThreadv4(" + localPort + ")",
                    IsBackground = true
                };
                _thrSocketReader.Start(_socket);
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

        private void Read(object state)
        {
            var socket = (Socket)state;
            EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var receiveBuffer = new byte[1500];
            while (_active)
            {
                try
                {
                    if (socket.Available == 0 && !socket.Poll(ReceivePollingTime, SelectMode.SelectRead))
                        continue;
                    
                    
                    var result = socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None,
                        ref bufferEndPoint);
               
                    Debug.Log($"[R]Received data from {bufferEndPoint}, result: {result}");
                    HandleBinaryData(receiveBuffer, receiveBuffer.Length);
                }
                catch (SocketException ex)
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

        public void Init(string outHost, int outPort, int inPort)
        {
            _sendEndPoint = IpNetUtils.MakeEndPoint(outHost, outPort);
            
            _inPort = inPort;

            _thrConnect = new Thread(InitThread);
            _thrConnect.Start();
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        public void Disconnect(string reason)
        {
            try
            {
                _socket.Close();
                _active = false;
                    
                _thrConnect.Abort();
                _thrSocketReader.Abort();
            }
            catch (Exception ex)
            {
                LogError(ex.Message, ex.StackTrace);
            }
        }

        public void Kill()
        {
            _socket?.Close();
            _active = false;
        }

        public void Write(byte[] data)
        {
            WriteSocket(data);
        }

        private void WriteSocket(byte[] buf)
        {
            if(!_active)
                return;
            
            try
            {
                _socket.SendTo(buf, 0, buf.Length, SocketFlags.None, _sendEndPoint);
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
            LogError(err, stacktrace);
            OnError(err, se);
        }
        
    }
}
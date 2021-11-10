using System;
using PdNetwork.Exchange;
using PdNetwork.Sockets.Impl;
using PdNetwork.Utils;

namespace PdNetwork.Client.Impl
{
    public class PdUdpClient
    {
        private UdpSocketLayer _socket;
        
        public Action<byte[]> OnResponse { get; set; }

        public void Configure(string outHost, int outPort, int inPort)
        {
            _socket = new UdpSocketLayer();
            _socket.Init(outHost, outPort, inPort);   
            
            _socket.OnData += OnSocketData;
        }

        public void Send(IRequest request)
        {
            var bytes = request.GetRequestBytes();
            _socket.Write(bytes);
        }

        public void Send(byte[] bytes)
        {
            _socket.Write(bytes);
        }

        public void Destroy()
        {
            _socket.OnData -= OnSocketData;
            _socket.Disconnect();
            _socket = null;
        }

        private void OnSocketData(byte[] data)
        {
            OnResponse?.Invoke(data);
        }
    }
}
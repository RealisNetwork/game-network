using System;
using System.Net.Sockets;

namespace PdNetwork.Sockets
{
    public interface ISocketLayer
    {
        void Connect(string host, int port);

        bool IsConnected { get; }

        bool RequiresConnection { get; }

        void Disconnect();

        void Disconnect(string reason);

        Action OnConnect { get; set; }

        Action<string> OnDisconnect { get; set; }

        void Write(byte[] data);

        Action<byte[]> OnData { get; set; }

        // OnStringDataDelegate OnStringData { get; set; }

        Action<string, SocketError> OnError { get; set; }

        void Kill();

        void Destroy();
    }
}
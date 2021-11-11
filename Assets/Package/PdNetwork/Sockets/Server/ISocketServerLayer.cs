using System;
using System.Net.Sockets;

namespace PdNetwork.Sockets.Server
{
	public interface ISocketServerLayer
	{
		void Start(int port);

		void Disconnect();

		void Disconnect(string reason);
		
		void DisconnectPeer(byte peer, string reason);

		Action OnStarted { get; set; }

		Action<byte> OnPeerConnect { get; set; }
		Action<byte, string> OnPeerDisconnect { get; set; }

		Action<string> OnDisconnect { get; set; }

		void Write(byte[] data);
		void Write(byte[] recipients, byte[] data);

		Action<byte[]> OnData { get; set; }

		Action<string, SocketError> OnError { get; set; }
	}
}
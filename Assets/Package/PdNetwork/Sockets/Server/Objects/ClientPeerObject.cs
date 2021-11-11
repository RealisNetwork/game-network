using System.Net.Sockets;
using System.Text;

namespace PdNetwork.Sockets.Server.Objects
{
	public class ClientPeerObject
	{
		public byte PeerId { get; private set; }
		protected internal NetworkStream Stream { get; set; }
		TcpClient client;

		public ClientPeerObject(TcpClient tcpClient, byte peerId)
		{
			PeerId = peerId;
			client = tcpClient;
		}

		protected internal void Close()
		{
			if (Stream != null)
				Stream.Close();
			if (client != null)
				client.Close();
		}
	}
}
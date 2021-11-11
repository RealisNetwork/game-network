using System;
using PdNetwork.Client;
using PdNetwork.Exchange;
using PdNetwork.Sockets.Server;

namespace PdNetwork.Engine.Impl
{
	public class PdNetEngineServer : IPdNetEngineServer
	{
		public bool IsConnected { get; }

		public Action OnConfigured { get; set; }

		public Action OnConnected { get; set; }

		public Action<string> OnConnectError { get; set; }

		public Action OnConnectionResume { get; set; }

		public Action<IRequest> OnRequest { get; set; }

		public Action<string> OnDisconnected { get; set; }
		
		private ISocketServerLayer _socketServerLayer;

		public void Configure(ISocketServerLayer socketServerLayer)
		{
			_socketServerLayer = socketServerLayer;
		}

		public void Start(int port)
		{
			_socketServerLayer.Start(port);
		}

		public void Destroy()
		{
			throw new NotImplementedException();
		}

		public void DisconnectPeer(byte peer)
		{
			throw new NotImplementedException();
		}

		public void Disconnect()
		{
			throw new NotImplementedException();
		}

		public void Send(byte[] recipients, IResponse request)
		{
			throw new NotImplementedException();
		}

		public void SendToAll(IResponse request)
		{
//			_socketServerLayer.Write(request.ge);
		}
	}
}
using System;
using PdNetwork.Client;
using PdNetwork.Exchange;
using PdNetwork.Sockets.Server;

namespace PdNetwork.Engine
{
	public interface IPdNetEngineServer
	{
		void Configure(ISocketServerLayer socketServerLayer);
		bool IsConnected { get; }

		Action OnConfigured { set; get; }
		Action OnConnected { set; get; }
		Action<string> OnConnectError { set; get; }
		Action OnConnectionResume { set; get; }
		Action<IRequest> OnRequest { get; set; }
		Action<string> OnDisconnected { set; get; }
        
		void Destroy();
		void Start(int port);
		void DisconnectPeer(byte peer);
		void Disconnect();
		void Send(byte[] recipients, IResponse request);
		void SendToAll(IResponse request);
	}
}
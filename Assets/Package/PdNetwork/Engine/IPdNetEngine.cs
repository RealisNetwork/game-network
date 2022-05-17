using System;
using PdNetwork.Client;
using PdNetwork.Exchange;
using PdNetwork.LagMonitor;

namespace PdNetwork.Engine
{
    public interface IPdNetEngine
    {
        void Configure(byte appId, string authToken, string apiVersion, ISocketClient socketClient, int reconnectionSeconds);
        void ConfigureUdp(string outHost, int outPort, int inPort);

        IPingMonitor PingMonitor { set; }
        bool IsConnecting { get; }
        bool IsConnected { get; }
        long AverageRoundPing { get; }

        Action OnConfigured { set; get; }
        Action OnConnected { set; get; }
        Action<string> OnConnectError { set; get; }
        Action OnConnectionResume { set; get; }
        Action<IResponse> OnResponse { get; set; }
        Action<byte[]> OnUdpResponse { get; set; }
        Action<int> OnReconnectionTry { get; set; }
        Action<string> OnDisconnected { set; get; }
        
        void Destroy();
        void Connect(string host, int port);
        void KillConnection();
        void Reconnect();
        void Disconnect();
        void Send(IRequest request);
        void SendUdp(IRequest request);
        void SendUdp(byte[] bytes);
    }
}
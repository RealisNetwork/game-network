using System;
using PdNetwork.Exchange;

namespace PdNetwork.Client
{
    public interface ISocketClient
    {
        bool Connected { get; }
        
        void Init();
        
        void Destroy();
        
        void Connect(string host, int port);
        
        void Disconnect(string reason = null);
        
        Action<bool> OnConnect { get; set; }

        Action<string> OnDisconnect { get; set; }
        
        Action<string> OnIoError { get; set; }
        Action<string> OnDataError { get; set; }
        Action<IResponse> OnResponse { get; set; }
        
        Action<int> OnReconnectionTry { get; set; }
        
        void KillConnection();
        
        int ReconnectionSeconds { get; set; }

        bool IsReconnecting { get; set; }

        void Send(IRequest request);
    }
}
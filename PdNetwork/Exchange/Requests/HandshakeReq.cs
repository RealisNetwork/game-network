using ByteFormatter.Runtime;

namespace PdNetwork.Exchange.Requests
{
    public readonly struct HandshakeReq : IRequest
    {
        private readonly string _authToken;
        private readonly string _apiVersion;
        private readonly string _sessionToken;

        public HandshakeReq(string authToken, string apiVersion, string sessionToken)
        {
            _authToken = authToken;
            _apiVersion = apiVersion;
            _sessionToken = sessionToken;
        }

        public byte GetHeader() => (byte) Header.Handshake;

        public void WriteBody(ByteWriter writer)
        {
            if (!string.IsNullOrEmpty(_authToken))
            {
                writer.Write(_authToken);   
            }
            if (!string.IsNullOrEmpty(_apiVersion))
            {
                writer.Write(_apiVersion);   
            }
            if (!string.IsNullOrEmpty(_sessionToken))
            {
                writer.Write(_sessionToken);
            }
        }
    }
}
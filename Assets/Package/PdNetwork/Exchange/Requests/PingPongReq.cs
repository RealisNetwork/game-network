using ByteFormatter.Runtime;

namespace PdNetwork.Exchange.Requests
{
    public readonly struct PingPongReq : IRequest
    {
        public byte GetHeader() => (byte) Header.PingPong;

        public void WriteBody(ByteWriter writer)
        {
            
        }
    }
}
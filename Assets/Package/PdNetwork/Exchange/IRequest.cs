using ByteFormatter.Runtime;

namespace PdNetwork.Exchange
{
    public interface IRequest
    {
        byte GetHeader();
        void WriteBody(ByteWriter writer);
    }
    
    public interface IRequestReader
    {
        IRequest ReadRequest(byte[] body);
    }
}
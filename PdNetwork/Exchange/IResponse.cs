namespace PdNetwork.Exchange
{
    public interface IResponse
    {
        byte GetHeader();
    }
    
    public interface IResponseReader
    {
        IResponse ReadResponse(byte[] body);
    }
}
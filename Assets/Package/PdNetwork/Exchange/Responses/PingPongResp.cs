
namespace PdNetwork.Exchange.Responses
{
    public readonly struct PingPongResp : IResponse
    {
        public byte GetHeader() => (byte) Header.PingPong;
        
        public readonly struct Reader : IResponseReader
        {
            public IResponse ReadResponse(byte[] body)
            {
                return new PingPongResp();
            }
        }
    }
}
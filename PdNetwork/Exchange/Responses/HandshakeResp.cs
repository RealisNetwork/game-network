using PdNetwork.Utils.Pools;

namespace PdNetwork.Exchange.Responses
{
    public readonly struct HandshakeResp : IResponse
    {
        public readonly bool Success;
        public readonly string SessionToken;
        public readonly string Error;
        public readonly ErrorCodeType? ErrorCode;

        private HandshakeResp(bool success, string sessionToken, string error, ErrorCodeType? errorCode)
        {
            Success = success;
            SessionToken = sessionToken;
            Error = error;
            ErrorCode = errorCode;
        }

        public enum ErrorCodeType : byte
        {
            WrongVersion = 1,
            InvalidSessionToken = 2,
            OtherError = 3
        }
        
        public byte GetHeader() => (byte) Header.Handshake;

        private static HandshakeResp CreateSuccess(string sessionToken)
        {
            return new HandshakeResp(true, sessionToken, string.Empty, null);
        }

        private static HandshakeResp CreateFail(string error, ErrorCodeType errorCodeType)
        {
            return new HandshakeResp(false, null, error, errorCodeType);
        }

        public readonly struct Reader : IResponseReader
        {
            public IResponse ReadResponse(byte[] body)
            {
                var reader = ByteReaderPool.Instance.GetReader(body);
                var success = reader.ReadBoolean();
                if (success)
                {
                    var sessionToken = reader.ReadString();
                    return CreateSuccess(sessionToken);
                }

                var error = reader.ReadString();
                var errorCode = reader.ReadByte();
                ByteReaderPool.Instance.Return(reader);
                return CreateFail(error, (ErrorCodeType) errorCode);
            }
        }
    }
}
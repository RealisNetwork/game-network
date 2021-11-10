using System.Text;
using PdNetwork.Exchange;
using PdNetwork.Utils.Pools;

namespace PdNetwork.Utils
{
    //byte array consists of:
    // - length: 2 bytes
    // - header: 1 byte
    // - body: any bytes
    // payload = body + header 
    public static class PdNetUtils
    {
        private static readonly StringBuilder StringBuilder = new StringBuilder();

        public static byte[] GetRequestBytes(this IRequest request)
        {
            var writer = ByteWriterPool.Instance.Get();
            
            const int lengthBytes = 2;
            writer.Skip(lengthBytes); //first 2 bytes for length (short)
            writer.Write(request.GetHeader());
            
            request.WriteBody(writer);

            // Debug.Log("GetBytes: before length: " + writer.ToArray().GetByteArrayAsString());
            
            var actualLength = writer.Position;
            var payloadLength = actualLength - lengthBytes;
            writer.SeekZero();
            writer.Write((short)payloadLength);
            writer.Skip((uint) payloadLength);
            
            // Debug.Log("GetBytes: after length: " + writer.ToArray().GetByteArrayAsString());
            
            var res = writer.ToArray();
            return res;
        }
        
        public static string GetByteArrayAsString(this byte[] bytes)
        {
            StringBuilder.Clear();
            var sb = StringBuilder.Append("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static sbyte[] ToSBytes(this byte[] bytes) 
        {
            var sBytes = new sbyte[bytes.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                var sByte = unchecked((sbyte)b);
                sBytes[i] = sByte;
            }
            return sBytes;
        }
    }
}
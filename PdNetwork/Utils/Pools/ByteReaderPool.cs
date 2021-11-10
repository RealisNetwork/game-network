using ByteFormatter.Runtime;

namespace PdNetwork.Utils.Pools
{
    public class ByteReaderPool : AObjectPool<ByteReader>
    {
        public static int InitialSize = 50;
        
        private static ByteReaderPool _instance;

        public static ByteReaderPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ByteReaderPool(InitialSize);
                    _instance.Init();
                }

                return _instance;
            }
        }

        public ByteReaderPool(int initialSize) : base(initialSize)
        {
        }

        protected override ByteReader Create() => new ByteReader();

        public ByteReader GetReader(byte[] bytes)
        {
            var reader = Get();
            reader.ReInit(bytes);
            return reader;
        }

        public override void Return(ByteReader item)
        {
            item.Reset();
            base.Return(item);
        }
    }
}
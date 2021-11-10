using ByteFormatter.Runtime;

namespace PdNetwork.Utils.Pools
{
    public class ByteWriterPool : AObjectPool<ByteWriter>
    {
        private static ByteWriterPool _instance;

        public static ByteWriterPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ByteWriterPool(50);
                    _instance.Init();                    
                }
                return _instance;
            }
        }
        
        private ByteWriterPool(int initialSize) : base(initialSize)
        {
        }

        protected override ByteWriter Create() => new ByteWriter();

        public override void Return(ByteWriter item)
        {
            item.Reset();
            base.Return(item);
        }
    }
}
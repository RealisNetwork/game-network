using System.Collections.Concurrent;

namespace PdNetwork.Utils.Pools
{
    public abstract class AObjectPool<T>
    {
        private readonly int _initialSize;
        private readonly ConcurrentBag<T> _objects = new ConcurrentBag<T>();

        protected AObjectPool(int initialSize)
        {
            _initialSize = initialSize;
        }

        protected void Init()
        {
            for (var i = 0; i < _initialSize; i++)
            {
                var item = Create();
                _objects.Add(item);
            }
        }
        
        public T Get() => _objects.TryTake(out var item) ? item : Create();

        public virtual void Return(T item) => _objects.Add(item);
        
        protected abstract T Create();
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace PdNetwork.Exchange
{
    public interface IExchangeManager
    {
        void Initialize();
        IResponse ReadResponse(byte header, byte[] body);
    }
    
    public abstract class AExchangeManager : IExchangeManager
    {
        private readonly IDictionary<byte, IResponseReader> _readers = new Dictionary<byte,IResponseReader>();
        
        public abstract void Initialize();

        protected void RegisterResponseReader(byte header, IResponseReader reader) {
            _readers.Add(header, reader);
        }

        public IResponse ReadResponse(byte header, byte[] body)
        {
            if(!_readers.ContainsKey(header)) {
                Debug.LogError("[EventHandler] -> no handler for header " + header);
                return null;
            }
            
            var reader = _readers[header];
            return reader.ReadResponse(body);
        }
    }
}
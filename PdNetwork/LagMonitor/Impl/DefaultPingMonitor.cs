using System;
using System.Collections.Generic;
using System.Timers;
using PdNetwork.Engine;
using PdNetwork.Exchange;
using PdNetwork.Exchange.Requests;
using PdNetwork.Exchange.Responses;

namespace PdNetwork.LagMonitor.Impl
{
    public class DefaultPingMonitor : IPingMonitor
    {
        private IPdNetEngine _netEngine;
        private readonly List<long> _valueQueue = new List<long>();
        private readonly Timer _pollTimer;
        private readonly int _queueSize;
        private long _lastReqTimeUnixTimeMs;

        public DefaultPingMonitor(IPdNetEngine netEngine, int interval, int queueSize)
        {
            if (interval < 1)
                interval = 1;
            _netEngine = netEngine;
            _queueSize = queueSize;
            _pollTimer = new Timer
            {
                Enabled = false, AutoReset = true
            };
            _pollTimer.Elapsed += OnPollEvent;
            _pollTimer.Interval = interval * 1000;
        }

        public long AverageRoundPing
        {
            get
            {
                if (_valueQueue.Count == 0)
                    return 0;
                
                var sum = 0L;
                foreach (var roundPing in _valueQueue)
                {
                    sum += roundPing;   
                }
                return sum / _valueQueue.Count;
            }
        }

        public void Start()
        {
            if (IsRunning)
                return;
            _pollTimer.Start();
            _netEngine.OnResponse += OnResponse;
        }

        private void OnResponse(IResponse response)
        {
            if (response is PingPongResp)
            {
                StoreRoundPing();
            }
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            _pollTimer.Stop();
        }

        public void Destroy()
        {
            Stop();
            _pollTimer.Dispose();
            _netEngine = null;
        }
        
        public bool IsRunning => _pollTimer.Enabled;

        private void StoreRoundPing()
        {
            if (_lastReqTimeUnixTimeMs == 0)
                return;
            
            var int32 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastReqTimeUnixTimeMs;
            if (_valueQueue.Count >= _queueSize)
                _valueQueue.RemoveAt(0);
            _valueQueue.Add(int32);
        }

        private void OnPollEvent(object source, ElapsedEventArgs e)
        {
            _lastReqTimeUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _netEngine.Send(new PingPongReq());
        }
    }
}
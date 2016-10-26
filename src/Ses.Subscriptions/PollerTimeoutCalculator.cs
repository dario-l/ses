using System;
using System.Collections.Generic;

namespace Ses.Subscriptions
{
    internal class PollerTimeoutCalculator
    {
        private const short minTimeoutValue = 20;
        private const short maxTimeoutLevel = 10000;
        private static readonly Dictionary<short, short> timeoutLevels = new Dictionary<short, short>
        {
            // count | ms
            { maxTimeoutLevel, 1000 },
            { 5000, 500 },
            { 2000, 250 },
            { 1500, 100 },
            { 1000, 60 },
            { 500, 40 },
            { 0, minTimeoutValue }
        };

        private short _notDispatchingCounter;
        private readonly TimeSpan _pollerStaticTimeout;

        public PollerTimeoutCalculator(TimeSpan pollerStaticTimeout)
        {
            _pollerStaticTimeout = pollerStaticTimeout;
        }

        public double CalculateNext(bool anyDispatched = true)
        {
            if (_pollerStaticTimeout != TimeSpan.Zero)
            {
                _notDispatchingCounter = 0;
                return anyDispatched ? minTimeoutValue : _pollerStaticTimeout.TotalMilliseconds;
            }

            if (!anyDispatched)
            {
                if (_notDispatchingCounter >= maxTimeoutLevel) return timeoutLevels[maxTimeoutLevel];

                var levelValue = minTimeoutValue;
                foreach (var level in timeoutLevels)
                {
                    if (_notDispatchingCounter <= level.Key) continue;
                    levelValue = level.Value;
                    break;
                }
                _notDispatchingCounter++;
                return levelValue;
            }
            _notDispatchingCounter = 0;
            return minTimeoutValue;
        }
    }
}
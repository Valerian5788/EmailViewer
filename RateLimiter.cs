using System;
using System.Collections.Concurrent;

namespace EmailViewer.Utilities
{
    public class RateLimiter
    {
        private readonly ConcurrentDictionary<string, DateTime> _lastAttemptTimes = new ConcurrentDictionary<string, DateTime>();
        private readonly TimeSpan _interval;
        private readonly int _maxAttempts;

        public RateLimiter(TimeSpan interval, int maxAttempts)
        {
            _interval = interval;
            _maxAttempts = maxAttempts;
        }

        public bool ShouldAllow(string key)
        {
            var now = DateTime.UtcNow;
            if (_lastAttemptTimes.TryGetValue(key, out var lastAttempt))
            {
                if (now - lastAttempt < _interval)
                {
                    return false;
                }
            }

            _lastAttemptTimes[key] = now;
            return true;
        }
    }
}
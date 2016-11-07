using System;

namespace Ses.Subscriptions
{
    public class RunnerInfo
    {
        public DateTime StartedAt { get; }
        public bool IsLockedByPolicy { get; }
        public bool IsRunning { get; }

        internal RunnerInfo(DateTime startedAt, bool isLockedByPolicy, bool isRunning)
        {
            StartedAt = startedAt;
            IsLockedByPolicy = isLockedByPolicy;
            IsRunning = isRunning;
        }
    }
}
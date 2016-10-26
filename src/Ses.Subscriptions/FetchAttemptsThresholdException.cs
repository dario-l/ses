using System;

namespace Ses.Subscriptions
{
    internal class FetchAttemptsThresholdException : Exception
    {
        public string PollerType { get; private set; }
        public int ExecuteRetryAttempts { get; private set; }

        public FetchAttemptsThresholdException(string pollerType, int executeRetryAttempts, Exception exception)
            : base($"Poller {pollerType} excides retries attempts threshold.", exception)
        {
            PollerType = pollerType;
            ExecuteRetryAttempts = executeRetryAttempts;
        }
    }
}
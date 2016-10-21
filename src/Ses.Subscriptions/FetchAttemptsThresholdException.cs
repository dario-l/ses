using System;

namespace Ses.Subscriptions
{
    internal class FetchAttemptsThresholdException : Exception
    {
        public string PoolerType { get; private set; }
        public int ExecuteRetryAttempts { get; private set; }

        public FetchAttemptsThresholdException(string poolerType, int executeRetryAttempts, Exception exception)
            : base($"Pooler {poolerType} excides retries attempts threshold.", exception)
        {
            PoolerType = poolerType;
            ExecuteRetryAttempts = executeRetryAttempts;
        }
    }
}
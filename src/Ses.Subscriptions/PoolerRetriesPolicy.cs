namespace Ses.Subscriptions
{
    public class PoolerRetriesPolicy
    {
        public int HandlerAttemptsThreshold { get; private set; }
        public int FetchAttemptsThreshold { get; private set; }

        public PoolerRetriesPolicy(int fetchAttemptsThreshold, int handlerAttemptsThreshold)
        {
            if (fetchAttemptsThreshold < 0) fetchAttemptsThreshold = 0;
            if (handlerAttemptsThreshold < 0) handlerAttemptsThreshold = 0;
            FetchAttemptsThreshold = fetchAttemptsThreshold;
            HandlerAttemptsThreshold = handlerAttemptsThreshold;
        }

        public static PoolerRetriesPolicy Defaut()
        {
            return new PoolerRetriesPolicy(3, 3);
        }

        public static PoolerRetriesPolicy NoRetries()
        {
            return new PoolerRetriesPolicy(0, 0);
        }
    }
}
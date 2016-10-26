namespace Ses.Subscriptions
{
    public class PollerRetriesPolicy
    {
        public int HandlerAttemptsThreshold { get; private set; }
        public int FetchAttemptsThreshold { get; private set; }

        public PollerRetriesPolicy(int fetchAttemptsThreshold, int handlerAttemptsThreshold)
        {
            if (fetchAttemptsThreshold < 0) fetchAttemptsThreshold = 0;
            if (handlerAttemptsThreshold < 0) handlerAttemptsThreshold = 0;
            FetchAttemptsThreshold = fetchAttemptsThreshold;
            HandlerAttemptsThreshold = handlerAttemptsThreshold;
        }

        public static PollerRetriesPolicy Defaut()
        {
            return new PollerRetriesPolicy(3, 3);
        }

        public static PollerRetriesPolicy NoRetries()
        {
            return new PollerRetriesPolicy(0, 0);
        }
    }
}
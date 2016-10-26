using System;

namespace Ses.Abstracts.Subscriptions
{
    public interface ISubscriptionPoller
    {
        ISubscriptionEventSource[] Sources { get; }
        TimeSpan? RunForDuration { get; }
        TimeSpan GetFetchTimeout();
    }
}
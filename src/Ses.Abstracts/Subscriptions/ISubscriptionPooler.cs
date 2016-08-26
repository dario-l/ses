using System;

namespace Ses.Abstracts.Subscriptions
{
    public interface ISubscriptionPooler
    {
        ISubscriptionEventSource[] Sources { get; }
        TimeSpan? RunForDuration { get; }
        TimeSpan GetFetchTimeout();
    }
}
namespace Ses.Abstracts.Subscriptions
{
    public interface IHandle
    {
    }

    public interface IHandle<in T> : IHandle where T : class, IEvent
    {
        void Handle(T e, EventEnvelope envelope);
    }
}
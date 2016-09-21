namespace Ses.Abstracts
{
    public interface IReadOnlyEventStream
    {
        IEvent[] CommittedEvents { get; }
        int CommittedVersion { get; }
    }
}
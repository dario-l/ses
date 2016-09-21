namespace Ses.Abstracts
{
    public class ReadOnlyEventStream : IReadOnlyEventStream
    {
        public ReadOnlyEventStream(IEvent[] events, int committedVersion)
        {
            CommittedEvents = events;
            CommittedVersion = committedVersion;
        }

        public IEvent[] CommittedEvents { get; }
        public int CommittedVersion { get; }
    }
}
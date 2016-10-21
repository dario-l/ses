using Ses.Abstracts;

namespace Ses.Domain
{
    public class AggregateSnapshot : IAggregateSnapshot
    {
        public AggregateSnapshot(int version, IMemento state)
        {
            Version = version;
            State = state;
        }

        public IMemento State { get; }
        public int Version { get; }
    }
}
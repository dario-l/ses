using Ses.Abstracts;

namespace Ses.Domain
{
    internal class AggregateSnapshot<TSnapshot> : IAggregateSnapshot<TSnapshot> where TSnapshot : class, IMemento, new()
    {
        public AggregateSnapshot(int version, TSnapshot state)
        {
            Version = version;
            State = state;
        }

        public TSnapshot State { get; }
        public int Version { get; }
    }
}
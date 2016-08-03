using Ses.Abstracts;

namespace Ses.Domain
{
    public interface IAggregateSnapshot<out TSnapshot> where TSnapshot : class, IMemento, new()
    {
        TSnapshot State { get; }
        int Version { get; }
    }
}
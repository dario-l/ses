using Ses.Abstracts;

namespace Ses.Domain
{
    public interface IAggregateSnapshot
    {
        IMemento State { get; }
        int Version { get; }
    }
}
using System;

namespace Ses.Abstracts
{
    public interface IEventStream : IReadOnlyEventStream
    {
        Guid CommitId { get; }
    }
}
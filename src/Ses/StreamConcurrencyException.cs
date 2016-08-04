using System;

namespace Ses
{
    public class StreamConcurrencyException : Exception
    {
        public Guid StreamId { get; private set; }
        public Guid CommitId { get; private set; }
        public int CommittedVersion { get; private set; }

        public StreamConcurrencyException(Exception innerException, Guid streamId, Guid commitId, int committedVersion)
            : base("Event concurrency violation detected on stream " + streamId, innerException)
        {
            StreamId = streamId;
            CommitId = commitId;
            CommittedVersion = committedVersion;
        }
    }
}
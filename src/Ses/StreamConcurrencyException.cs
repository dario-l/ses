using System;

namespace Ses
{
    public class StreamConcurrencyException : Exception
    {
        public Guid StreamId { get; private set; }
        public Guid CommitId { get; private set; }
        public int CommittedVersion { get; private set; }
        public int ConflictedEventVersion { get; private set; }
        public Type ConflictedEventType { get; private set; }
        public string ConflictedEventContractName { get; private set; }

        public StreamConcurrencyException(Exception innerException, Guid streamId, Guid commitId, int committedVersion, int conflictedEventVersion, Type conflictedEventType, string conflictedEventContractName)
            : base("Event concurrency violation detected on stream " + streamId, innerException)
        {
            StreamId = streamId;
            CommitId = commitId;
            CommittedVersion = committedVersion;
            ConflictedEventVersion = conflictedEventVersion;
            ConflictedEventType = conflictedEventType;
            ConflictedEventContractName = conflictedEventContractName;
        }
    }
}
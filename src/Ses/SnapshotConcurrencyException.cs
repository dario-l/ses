using System;

namespace Ses
{
    public class SnapshotConcurrencyException : Exception
    {
        public Guid StreamId { get; private set; }
        public int Version { get; private set; }
        public string ContractName { get; private set; }

        public SnapshotConcurrencyException(Exception innerException, Guid streamId, int version, string contractName)
            : base("Snapshot concurrency violation detected on stream " + streamId, innerException)
        {
            StreamId = streamId;
            Version = version;
            ContractName = contractName;
        }
    }
}
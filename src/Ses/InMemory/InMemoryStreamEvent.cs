using System;

namespace Ses.InMemory
{
    internal sealed class InMemoryEventRecord
    {
        public InMemoryEventRecord(int version, string contractName, byte[] eventData, Guid commitId, byte[] metadata)
        {
            Version = version;
            ContractName = contractName;
            EventData = eventData;
            CommitId = commitId;
            Metadata = metadata;
        }

        public int Version { get; private set; }
        public string ContractName { get; private set; }
        public byte[] EventData { get; private set; }
        public Guid CommitId { get; private set; }
        public byte[] Metadata { get; private set; }
    }
}
namespace Ses.InMemory
{
    internal class InMemorySnapshot
    {
        public InMemorySnapshot(int version, string contractName, byte[] payload)
        {
            Version = version;
            ContractName = contractName;
            Payload = payload;
        }

        public int Version { get; private set; }
        public byte[] Payload { get; private set; }
        public string ContractName { get; private set; }

        public void Update(int version, byte[] payload)
        {
            Version = version;
            Payload = payload;
        }
    }
}
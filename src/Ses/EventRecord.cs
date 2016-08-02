namespace Ses
{
    public struct EventRecord
    {
        public EventRecord(int version, string contractName, byte[] payload)
        {
            Version = version;
            ContractName = contractName;
            Payload = payload;
        }

        public int Version { get; private set; }
        public string ContractName { get; private set; }
        public byte[] Payload { get; private set; }
    }
}
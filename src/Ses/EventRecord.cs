namespace Ses
{
    public struct EventRecord
    {
        private EventRecord(string contractName, int version, byte[] payload, RecordKind kind)
        {
            ContractName = contractName;
            Version = version;
            Payload = payload;
            Kind = kind;
        }

        public int Version { get; private set; }
        public string ContractName { get; private set; }
        public byte[] Payload { get; private set; }
        public RecordKind Kind { get; private set; }

        public static EventRecord Null => new EventRecord(null, -1, null, RecordKind.Null);

        public static EventRecord Event(string contractName, int version, byte[] payload)
        {
            return new EventRecord(contractName, version, payload, RecordKind.Event);
        }

        public static EventRecord Snapshot(string contractName, int version, byte[] payload)
        {
            return new EventRecord(contractName, version, payload, RecordKind.Snapshot);
        }

        public enum RecordKind
        {
            Null = 0,
            Snapshot = 1,
            Event = 2
        }
    }
}
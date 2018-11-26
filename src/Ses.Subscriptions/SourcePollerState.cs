namespace Ses.Subscriptions
{
    public class SourcePollerState
    {
        public SourcePollerState(string pollerContractName, string sourceContractName, long sequenceId)
        {
            PollerContractName = pollerContractName;
            SourceContractName = sourceContractName;
            EventSequenceId = sequenceId;
        }

        public string PollerContractName { get; }
        public string SourceContractName { get; }
        public long EventSequenceId { get; private set; }

        public void SetEventSequenceId(long sequenceId)
        {
            EventSequenceId = sequenceId;
            IsDirty = true;
        }

        public bool IsDirty { get; private set; }

        public void Clear()
        {
            IsDirty = false;
        }

        public override string ToString()
        {
            return $"{PollerContractName}/{SourceContractName} = {EventSequenceId}";
        }
    }
}
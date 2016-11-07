namespace Ses.Subscriptions
{
    public class SourcePollerState
    {
        public SourcePollerState(string pollerContractName, string sourceContractName)
        {
            PollerContractName = pollerContractName;
            SourceContractName = sourceContractName;
        }

        public string PollerContractName { get; }
        public string SourceContractName { get; }
        public long EventSequenceId { get; set; }

        public override string ToString()
        {
            return $"{PollerContractName}/{SourceContractName} = {EventSequenceId}";
        }
    }
}
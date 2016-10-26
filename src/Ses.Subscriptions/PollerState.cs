namespace Ses.Subscriptions
{
    public class PollerState
    {
        public PollerState(string pollerContractName, string sourceContractName, string handlerContractName)
        {
            PollerContractName = pollerContractName;
            SourceContractName = sourceContractName;
            HandlerContractName = handlerContractName;
        }

        public string PollerContractName { get; }
        public string SourceContractName { get; }
        public string HandlerContractName { get; }
        public long EventSequenceId { get; set; }

        public override string ToString()
        {
            return $"{PollerContractName}/{SourceContractName}/{HandlerContractName} = {EventSequenceId}";
        }
    }
}
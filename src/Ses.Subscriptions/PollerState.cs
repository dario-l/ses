namespace Ses.Subscriptions
{
    public class PollerState : SourcePollerState
    {
        public PollerState(string pollerContractName, string sourceContractName, string handlerContractName, long sequenceId = 0)
            : base(pollerContractName, sourceContractName, sequenceId)
        {
            HandlerContractName = handlerContractName;
        }

        public string HandlerContractName { get; }

        public override string ToString()
        {
            return $"{PollerContractName}/{SourceContractName}/{HandlerContractName} = {EventSequenceId}";
        }
    }
}
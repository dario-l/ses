namespace Ses.Subscriptions
{
    public class PollerState : SourcePollerState
    {
        public PollerState(string pollerContractName, string sourceContractName, string handlerContractName)
            : base(pollerContractName, sourceContractName)
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
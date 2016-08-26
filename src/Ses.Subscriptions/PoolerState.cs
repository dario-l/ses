namespace Ses.Subscriptions
{
    public class PoolerState
    {
        public PoolerState(string poolerContractName, string sourceContractName, string handlerContractName)
        {
            PoolerContractName = poolerContractName;
            SourceContractName = sourceContractName;
            HandlerContractName = handlerContractName;
        }

        public string PoolerContractName { get; }
        public string SourceContractName { get; }
        public string HandlerContractName { get; }
        public long EventSequenceId { get; set; }

        public override string ToString()
        {
            return $"{PoolerContractName}/{SourceContractName}/{HandlerContractName} = {EventSequenceId}";
        }
    }
}
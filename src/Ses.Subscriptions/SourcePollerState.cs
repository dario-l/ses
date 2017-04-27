namespace Ses.Subscriptions
{
    public class SourcePollerState
    {
        private long _eventSequenceId;

        public SourcePollerState(string pollerContractName, string sourceContractName)
        {
            PollerContractName = pollerContractName;
            SourceContractName = sourceContractName;
        }

        public string PollerContractName { get; }
        public string SourceContractName { get; }

        public long EventSequenceId
        {
            get { return _eventSequenceId; }
            set
            {
                _eventSequenceId = value;
                IsDirty = true;
            }
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
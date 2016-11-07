using System;

namespace Ses.Subscriptions
{
    public class PollerInfo
    {
        public RunnerInfo Runner { get; }
        public Type PollerType { get; }
        public string PollerContractName { get; }
        public SourcePollerState[] SourceSequenceInfo { get; }

        internal PollerInfo(RunnerInfo runner, Type pollerType, string pollerContractName, SourcePollerState[] sourceSequenceInfo)
        {
            Runner = runner;
            PollerType = pollerType;
            PollerContractName = pollerContractName;
            SourceSequenceInfo = sourceSequenceInfo;
        }

        public override string ToString()
        {
            return $"Poller {PollerContractName} Locked:{Runner.IsLockedByPolicy}/Running:{Runner.IsRunning}";
        }
    }
}
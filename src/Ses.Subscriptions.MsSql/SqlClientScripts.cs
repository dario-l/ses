namespace Ses.Subscriptions.MsSql
{
    internal class SqlClientScripts
    {
        public const string InsertState = @"INSERT INTO [StreamsSubscriptionStates]([PoolerContractName],[SourceContractName],[HandlerContractName],[EventSequence])VALUES(@PoolerContractName,@SourceContractName,@HandlerContractName,@EventSequence)";
        public const string UpdateState = @"UPDATE [StreamsSubscriptionStates] SET [EventSequence]=@EventSequence WHERE [PoolerContractName]=@PoolerContractName AND [SourceContractName]=@SourceContractName AND [HandlerContractName]=@HandlerContractName";
        public const string SelectStates = @"SELECT [PoolerContractName], [SourceContractName],[HandlerContractName],[EventSequence] FROM [StreamsSubscriptionStates] WHERE [PoolerContractName] = @PoolerContractName";
        public const string DeleteNotUsedStates = @"DELETE FROM StreamsSubscriptionStates WHERE (([HandlerContractName] NOT IN (@HandlerContractNames)) OR ([SourceContractName] NOT IN (@SourceContractNames))) AND [PoolerContractName] = @PoolerContractName";


        public const string ParamPoolerContractName = "@PoolerContractName";
        public const string ParamSourceContractName = "@SourceContractName";
        public const string ParamHandlerContractName = "@HandlerContractName";
        public const string ParamEventSequence = "@EventSequence";
        public const string ParamHandlerContractNames = ParamHandlerContractName + "s";
        public const string ParamSourceContractNames = ParamSourceContractName + "s";
    }
}
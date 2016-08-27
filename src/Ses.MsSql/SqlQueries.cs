namespace Ses.MsSql
{
    internal static class SqlQueries
    {
        private const string streamId = "@StreamId";

        internal static class SelectEvents
        {
            public const string Query = "SesSelectEvents";
            public const string ParamStreamId = streamId;
            public const string ParamFromVersion = "@FromVersion";
            public const string ParamPessimisticLock = "@PessimisticLock";
        }

        internal static class DeleteStream
        {
            public const string QueryAny = "SesDeleteStreamAny";
            public const string QueryExpectedVersion = "SesDeleteStreamExpectedVersion";

            public const string ParamStreamId = streamId;
            public const string ParamExpectedVersion = "@ExpectedVersion";
        }

        internal static class UpdateSnapshot
        {
            public const string Query = "SesUpdateSnapshot";

            public const string ParamStreamId = streamId;
            public const string ParamVersion = "@Version";
            public const string ParamContractName = "@ContractName";
            public const string ParamGeneratedAtUtc = "@GeneratedAtUtc";
            public const string ParamPayload = "@Payload";
        }

        internal static class InsertEvents
        {
            public const string QueryNoStream = "SesInsertEventsNoStream";
            public const string QueryAny = "SesInsertEventsAny";
            public const string QueryExpectedVersion = "SesInsertEventsExpectedVersion";

            public const string ParamStreamId = streamId;
            public const string ParamCommitId = "@CommitId";
            public const string ParamMetadataPayload = "@MetadataPayload";
            public const string ParamIsLockable = "@IsLockable";
            public const string ParamExpectedVersion = "@ExpectedVersion";
            public const string ParamCreatedAtUtc = "@CreatedAtUtc";
            public const string ParamEventContractName = "@EventContractName";
            public const string ParamEventVersion = "@EventVersion";
            public const string ParamEventPayload = "@EventPayload";
        }

        internal static class Linearize
        {
            public const string Query = "SesLinearize";
        }
    }
}
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

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
            public const string QueryAny = @"
                BEGIN TRANSACTION DeleteStreamAny;
                    DELETE FROM [StreamsSnapshots] WHERE [StreamId]=@StreamId
                    DELETE FROM [StreamsMetadata] WHERE [StreamId]=@StreamId
                    DELETE FROM [Streams] WHERE [StreamId]=@StreamId
                COMMIT TRANSACTION DeleteStreamAny;";

            public const string QueryExpectedVersion = @"
                BEGIN TRANSACTION DeleteStreamExpectedVersion;
                    DELETE FROM [StreamsSnapshots] WHERE [StreamId]=@StreamId AND (SELECT COUNT([Version]) FROM [Streams] WHERE [Version] >= @ExpectedVersion AND [StreamId] = @StreamId) = 1
                    IF @@ROWCOUNT = 0 BEGIN
                        RAISERROR('WrongExpectedVersion', 16, 1);
                        RETURN;
                    END
                    DELETE FROM [StreamsMetadata] WHERE [StreamId]=@StreamId
                    DELETE FROM [Streams] WHERE [StreamId]=@StreamId
                COMMIT TRANSACTION DeleteStreamExpectedVersion";

            public const string ParamStreamId = streamId;
            public const string ParamExpectedVersion = "@ExpectedVersion";
        }

        internal static class UpdateSnapshot
        {
            public const string Query = @"
                UPDATE [StreamsSnapshots] SET GeneratedAtUtc = @GeneratedAtUtc, Version = @Version, Payload = @Payload, ContractName = @ContractName WHERE [StreamId] = @StreamId
                IF @@ROWCOUNT = 0 BEGIN
                    RAISERROR('WrongExpectedVersion', 16, 1);
                    RETURN;
                END";

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
    }
}
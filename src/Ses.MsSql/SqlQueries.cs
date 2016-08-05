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

            public const string QueryAny = @"
                BEGIN TRANSACTION SaveChangesAny;
                    DECLARE @IsNew BIT = 0
                    IF (SELECT COUNT(1) FROM [Streams] WHERE [StreamId] = @StreamId) > 0 BEGIN
                        SET @IsNew = 1
                    END

                    IF(@IsNew = 1 AND @IsLockable = 1) BEGIN
                        INSERT INTO [Streams]([StreamId],[CommitId],[Version],[ContractName],[CreatedAtUtc],[Payload])
                        VALUES(@StreamId,@CommitId,0,'Lockable',@CreatedAtUtc,0);
                    END

                    INSERT INTO [Streams]([StreamId],[Version],[CommitId],[ContractName],[Payload],[CreatedAtUtc])
                    SELECT
                        @StreamId,
                        [Version],
                        @CommitId,
                        [ContractName],
                        [Payload],
                        @CreatedAtUtc
                    FROM
                        @Events
                    ORDER BY
                        [Version] ASC;

                    IF(@MetadataPayload IS NOT NULL AND LEN(@MetadataPayload) > 0) BEGIN
                        INSERT INTO [StreamsMetadata]([StreamId],[CommitId],[Payload])
                        VALUES(@StreamId,@CommitId,@MetadataPayload);
                    END

                    IF(@IsNew = 1 AND @IsLockable = 1) BEGIN
                        INSERT INTO [StreamsSnapshots]([StreamId],[Version],[LastStreamVersion],[ContractName],[GeneratedAtUtc],[Payload])
                        VALUES(@StreamId,0,0,'Init',@CreatedAtUtc,0);
                    END
                COMMIT TRANSACTION SaveChangesAny;
            ";

            public const string QueryExpectedVersion = @"
                BEGIN TRANSACTION SaveChangesExpectedVersion;
                    IF(SELECT TOP 1 1 FROM [Streams] WHERE [StreamId] = @StreamId AND (SELECT TOP 1 [Version] FROM [Streams] WHERE [StreamId] = @StreamId ORDER BY [Version] DESC) = @ExpectedVersion) <> 1 BEGIN
                        RAISERROR('WrongExpectedVersion', 16, 1);
                        RETURN;
                    END

                    INSERT INTO [Streams]([StreamId],[Version],[CommitId],[ContractName],[Payload],[CreatedAtUtc])
                    SELECT
                        @StreamId,
                        [Version],
                        @CommitId,
                        [ContractName],
                        [Payload],
                        @CreatedAtUtc
                    FROM
                        @Events
                    ORDER BY
                        [Version] ASC;

                    IF(@MetadataPayload IS NOT NULL AND LEN(@MetadataPayload) > 0) BEGIN
                        INSERT INTO [StreamsMetadata]([StreamId],[CommitId],[Payload])
                        VALUES(@StreamId,@CommitId,@MetadataPayload);
                    END
                COMMIT TRANSACTION SaveChangesExpectedVersion;";

            public const string ParamStreamId = streamId;
            public const string ParamCommitId = "@CommitId";
            public const string ParamMetadataPayload = "@MetadataPayload";
            public const string ParamIsLockable = "@IsLockable";
            public const string ParamExpectedVersion = "@ExpectedVersion";
            public const string ParamCreatedAtUtc = "@CreatedAtUtc";

            public static SqlParameter ParamEvents => new SqlParameter(paramEvents, SqlDbType.Structured)
            {
                TypeName = newEventsSqlTypeName
            };

            private const string paramEvents = "@Events";
            private const string newEventsSqlTypeName = "dbo.NewEvents";

            private static readonly SqlMetaData[] newEventsSqlMetaData = 
            {
                new SqlMetaData("Version", SqlDbType.Int, false, false, SortOrder.Ascending, 0),
                new SqlMetaData("ContractName", SqlDbType.NVarChar, 225),
                new SqlMetaData("Payload", SqlDbType.VarBinary, SqlMetaData.Max),
            };

            public static IEnumerable<SqlDataRecord> CreateSqlDataRecords(EventRecord[] events)
            {
                var records = new List<SqlDataRecord>(events.Length);
                foreach (var msg in events)
                {
                    var record = new SqlDataRecord(newEventsSqlMetaData);
                    record.SetInt32(0, msg.Version);
                    record.SetString(1, msg.ContractName);
                    record.SetBytes(2, 0, msg.Payload, 0, msg.Payload.Length);
                    records.Add(record);
                }
                return records;
            }
        }
    }
}
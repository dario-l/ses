using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace Ses.MsSql
{
    internal static class SqlQueries
    {
        private const string streamId = "@StreamId";

        internal static class SelectStreamEvents
        {
            public const string Query = @"";

            public const string ParamStreamId = streamId;
            public const string ParamFromVersion = "@FromVersion";
            public const string ParamPessimisticLock = "@PessimisticLock";
        }

        internal static class DeleteStream
        {
            public const string QueryWhereExpectedVersionAny = @"
                DELETE FROM [StreamsSnapshots] WHERE [StreamId]=@StreamId
                DELETE FROM [StreamsMetadata] WHERE [StreamId]=@StreamId
                DELETE FROM [Streams] WHERE [StreamId]=@StreamId";

            public const string QueryWithExpectedVersion = @"
                DELETE FROM [StreamsSnapshots] WHERE [StreamId]=@StreamId AND (SELECT COUNT([Version]) FROM [Streams] WHERE [Version] >= @ExpectedVersion AND [StreamId] = @StreamId) = 1
                IF @@ROWCOUNT = 0 BEGIN
                    RAISERROR('WrongExpectedVersion', 16, 1);
                    RETURN;
                END
                DELETE FROM [StreamsMetadata] WHERE [StreamId]=@StreamId
                DELETE FROM [Streams] WHERE [StreamId]=@StreamId";

            public const string ParamStreamId = streamId;
            public const string ParamExpectedVersion = "@ExpectedVersion";
        }

        internal static class InsertSnapshot
        {
            public const string Query = @"";

            public const string ParamStreamId = streamId;
            public const string ParamVersion = "@Version";
            public const string ParamContractName = "@ContractName";
            public const string ParamGeneratedAtUtc = "@GeneratedAtUtc";
            public const string ParamPayload = "@Payload";
        }

        internal static class InsertEvents
        {
            public const string Query = @"";

            public const string ParamStreamId = streamId;
            public const string ParamCommitId = "@CommitId";
            public const string ParamMetadataPayload = "@MetadataPayload";

            public static SqlParameter ParamEvents => new SqlParameter(paramEvents, SqlDbType.Structured)
            {
                TypeName = newEventsSqlTypeName
            };

            private const string paramEvents = "@Events";
            private const string newEventsSqlTypeName = "NewEvents";

            private static readonly SqlMetaData[] newEventsSqlMetaData = 
            {
                new SqlMetaData("Version", SqlDbType.Int, true, false, SortOrder.Unspecified, -1),
                new SqlMetaData("ContractName", SqlDbType.NVarChar, 225),
                new SqlMetaData("Payload", SqlDbType.VarBinary, SqlMetaData.Max),
            };

            public static IEnumerable<SqlDataRecord> CreateSqlDataRecords(IEnumerable<EventRecord> events)
            {
                var records = new List<SqlDataRecord>();
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
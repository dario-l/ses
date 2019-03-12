using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Extensions;
// ReSharper disable PossibleNullReferenceException

namespace Ses.MsSql
{
    internal partial class MsSqlPersistor
    {
        public async Task<EventRecord[]> LoadAsync(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = new CancellationToken())
        {
            List<EventRecord> list = null;
            using (var cnn = new SqlConnection(_connectionString))
            {
                using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.SelectEvents.Query, cancellationToken).NotOnCapturedContext())
                {
                    cmd
                        .AddInputParam(SqlQueries.SelectEvents.ParamStreamId, DbType.Guid, streamId)
                        .AddInputParam(SqlQueries.SelectEvents.ParamFromVersion, DbType.Int32, fromVersion)
                        .AddInputParam(SqlQueries.SelectEvents.ParamPessimisticLock, DbType.Boolean, pessimisticLock);

                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).NotOnCapturedContext())
                    {
                        if (reader.HasRows) list = new List<EventRecord>(100);

                        while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext()) // read snapshot
                        {
                            if (await reader.IsDBNullAsync(colIndexForContractName, cancellationToken).NotOnCapturedContext()) break;

                            list.Add(EventRecord.Snapshot(
                                await reader.GetFieldValueAsync<string>(colIndexForContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<int>(colIndexForVersion, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<byte[]>(colIndexForPayload, cancellationToken).NotOnCapturedContext()));
                        }

                        await reader.NextResultAsync(cancellationToken).NotOnCapturedContext();

                        if (list == null && reader.HasRows) list = new List<EventRecord>(30);

                        while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext()) // read events
                        {
                            list.Add(EventRecord.Event(
                                await reader.GetFieldValueAsync<string>(colIndexForContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<int>(colIndexForVersion, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<byte[]>(colIndexForPayload, cancellationToken).NotOnCapturedContext()));
                        }
                    }
                }
            }
            return list?.ToArray() ?? new EventRecord[0];
        }

        public async Task DeleteStreamAsync(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                var query = expectedVersion == ExpectedVersion.Any
                    ? SqlQueries.DeleteStream.QueryAny
                    : SqlQueries.DeleteStream.QueryExpectedVersion;

                using (var cmd = await cnn.OpenAndCreateCommandAsync(query, cancellationToken).NotOnCapturedContext())
                {
                    try
                    {
                        cmd.AddInputParam(SqlQueries.DeleteStream.ParamStreamId, DbType.Guid, streamId);
                        if (expectedVersion != ExpectedVersion.Any)
                            cmd.AddInputParam(SqlQueries.DeleteStream.ParamExpectedVersion, DbType.Int32, expectedVersion);

                        await cmd.ExecuteNonQueryAsync(cancellationToken)
                            .NotOnCapturedContext();
                    }
                    catch (SqlException ex) when (ex.Message.StartsWith("WrongExpectedVersion"))
                    {
                        throw new WrongExpectedVersionException(
                            $"Deleting stream {streamId} error",
                            expectedVersion,
                            null,
                            ex);
                    }
                }
            }
        }

        public async Task UpdateSnapshotAsync(Guid streamId, int version, string contractName, byte[] payload, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.UpdateSnapshot.Query, cancellationToken).NotOnCapturedContext())
                    {
                        await cmd
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamStreamId, DbType.Guid, streamId)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamVersion, DbType.Int32, version)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamContractName, DbType.AnsiString, contractName)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamGeneratedAtUtc, DbType.DateTime, DateTime.UtcNow)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamPayload, DbType.Binary, payload)
                            .ExecuteNonQueryAsync(cancellationToken)
                            .NotOnCapturedContext();
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Message.StartsWith("WrongExpectedVersion"))
                    {
                        throw new WrongExpectedVersionException(
                            $"Updating snapshot for stream {streamId} error",
                            version,
                            contractName,
                            ex);
                    }
                    throw;
                }
            }
        }

        public async Task SaveChangesAsync(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata, bool isLockable, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                switch (expectedVersion)
                {
                    case ExpectedVersion.NoStream:
                        await SaveChangesNoStreamAsync(cnn, events, streamId, commitId, metadata, isLockable, cancellationToken);
                        break;
                    case ExpectedVersion.Any:
                        await SaveChangesAnyAsync(cnn, events, streamId, commitId, metadata, isLockable, cancellationToken);
                        break;
                    default:
                        await SaveChangesExpectedVersionAsync(cnn, events, streamId, commitId, expectedVersion, metadata, cancellationToken);
                        break;
                }
            }

            _linearizer?.Start();
        }

        private static async Task SaveChangesNoStreamAsync(SqlConnection cnn, EventRecord[] events, Guid streamId, Guid commitId, byte[] metadata, bool isLockable, CancellationToken cancellationToken)
        {
            var currentRecord = EventRecord.Null;
            using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.InsertEvents.QueryNoStream, cancellationToken).NotOnCapturedContext())
            {
                try
                {
                    cmd
                        .AddInputParam(SqlQueries.InsertEvents.ParamStreamId, DbType.Guid, streamId)
                        .AddInputParam(SqlQueries.InsertEvents.ParamCommitId, DbType.Guid, commitId)
                        .AddInputParam(SqlQueries.InsertEvents.ParamCreatedAtUtc, DbType.DateTime, DateTime.UtcNow)
                        .AddInputParam(SqlQueries.InsertEvents.ParamMetadataPayload, DbType.Binary, metadata, true)
                        .AddInputParam(SqlQueries.InsertEvents.ParamIsLockable, DbType.Boolean, isLockable)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventContractName, DbType.String, null)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventVersion, DbType.Int32, null)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventPayload, DbType.Binary, null);

                    foreach (var record in events)
                    {
                        currentRecord = record;
                        cmd.Parameters[5].Value = record.ContractName;
                        cmd.Parameters[6].Value = record.Version;
                        cmd.Parameters[7].Value = record.Payload;

                        await cmd.ExecuteNonQueryAsync(cancellationToken)
                            .NotOnCapturedContext();

                        cmd.Parameters[3].Value = DBNull.Value; // metadata is one per CommitId
                    }

                }
                catch (SqlException ex)
                {
                    if (ex.IsUniqueConstraintViolation() || ex.IsWrongExpectedVersionRised())
                    {
                        throw new WrongExpectedVersionException(
                            $"Saving new stream {streamId} error. Stream exists.",
                            currentRecord.Version,
                            currentRecord.ContractName,
                            ex);
                    }
                    throw;
                }
            }
        }

        private static async Task SaveChangesAnyAsync(SqlConnection cnn, EventRecord[] events, Guid streamId, Guid commitId, byte[] metadata, bool isLockable, CancellationToken cancellationToken)
        {
            var currentRecord = EventRecord.Null;

            using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.InsertEvents.QueryAny, cancellationToken).NotOnCapturedContext())
            {
                try
                {
                    cmd
                        .AddInputParam(SqlQueries.InsertEvents.ParamStreamId, DbType.Guid, streamId)
                        .AddInputParam(SqlQueries.InsertEvents.ParamCommitId, DbType.Guid, commitId)
                        .AddInputParam(SqlQueries.InsertEvents.ParamCreatedAtUtc, DbType.DateTime, DateTime.UtcNow)
                        .AddInputParam(SqlQueries.InsertEvents.ParamMetadataPayload, DbType.Binary, metadata, true)
                        .AddInputParam(SqlQueries.InsertEvents.ParamIsLockable, DbType.Boolean, isLockable)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventContractName, DbType.String, null)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventVersion, DbType.Int32, null)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventPayload, DbType.Binary, null);

                    foreach (var record in events)
                    {
                        currentRecord = record;
                        cmd.Parameters[5].Value = record.ContractName;
                        cmd.Parameters[6].Value = record.Version;
                        cmd.Parameters[7].Value = record.Payload;

                        await cmd.ExecuteNonQueryAsync(cancellationToken)
                            .NotOnCapturedContext();

                        cmd.Parameters[3].Value = DBNull.Value; // metadata is one per CommitId
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.IsUniqueConstraintViolation() || ex.IsWrongExpectedVersionRised())
                    {
                        throw new WrongExpectedVersionException(
                            $"Saving new or existing stream {streamId} error. Stream exists.",
                            currentRecord.Version,
                            currentRecord.ContractName,
                            ex);
                    }
                    throw;
                }
            }

        }

        private static async Task SaveChangesExpectedVersionAsync(SqlConnection cnn, EventRecord[] events, Guid streamId, Guid commitId, int expectedVersion, byte[] metadata, CancellationToken cancellationToken)
        {
            var currentRecord = EventRecord.Null;
            using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.InsertEvents.QueryExpectedVersion, cancellationToken).NotOnCapturedContext())
            {
                try
                {
                    cmd
                        .AddInputParam(SqlQueries.InsertEvents.ParamStreamId, DbType.Guid, streamId)
                        .AddInputParam(SqlQueries.InsertEvents.ParamCommitId, DbType.Guid, commitId)
                        .AddInputParam(SqlQueries.InsertEvents.ParamCreatedAtUtc, DbType.DateTime, DateTime.UtcNow)
                        .AddInputParam(SqlQueries.InsertEvents.ParamMetadataPayload, DbType.Binary, metadata, true)
                        .AddInputParam(SqlQueries.InsertEvents.ParamExpectedVersion, DbType.Int32, expectedVersion)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventContractName, DbType.String, null)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventVersion, DbType.Int32, null)
                        .AddInputParam(SqlQueries.InsertEvents.ParamEventPayload, DbType.Binary, null);

                    foreach (var record in events)
                    {
                        currentRecord = record;
                        cmd.Parameters[5].Value = record.ContractName;
                        cmd.Parameters[6].Value = record.Version;
                        cmd.Parameters[7].Value = record.Payload;

                        await cmd.ExecuteNonQueryAsync(cancellationToken)
                            .NotOnCapturedContext();

                        cmd.Parameters[3].Value = DBNull.Value; // metadata is one per CommitId
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.IsUniqueConstraintViolation() || ex.IsWrongExpectedVersionRised())
                    {
                        throw new WrongExpectedVersionException(
                            $"Saving new or existing stream {streamId} error. Stream exists.",
                            currentRecord.Version,
                            currentRecord.ContractName,
                            ex);
                    }
                    throw;
                }
            }
        }

        public async Task<int> GetStreamVersionAsync(Guid streamId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.GetStreamVersion.Query, cancellationToken))
                {
                    cmd.AddInputParam(SqlQueries.GetStreamVersion.ParamStreamId, DbType.Guid, streamId);
                    var version = await cmd.ExecuteScalarAsync(cancellationToken);
                    return version == null || version == DBNull.Value ? -1 : (int)version;
                }
            }
        }
    }
}

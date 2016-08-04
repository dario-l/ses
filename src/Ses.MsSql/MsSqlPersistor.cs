using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.MsSql
{
    internal class MsSqlPersistor : IEventStreamPersistor
    {
        private const short duplicateKeyViolationErrorNumber = 2627;
        private readonly string _connectionString;

        public MsSqlPersistor(string connectionString)
        {
            _connectionString = connectionString;
        }

        public event OnReadEventHandler OnReadEvent;
        public event OnReadSnapshotHandler OnReadSnapshot;

        public async Task<IList<IEvent>> Load(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = new CancellationToken())
        {
            var list = new List<IEvent>(50);
            using (var cnn = new SqlConnection(_connectionString))
            {
                using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.SelectStreamEvents.Query, cancellationToken).ConfigureAwait(false))
                {
                    cmd
                        .AddInputParam(SqlQueries.SelectStreamEvents.ParamStreamId, DbType.Guid, streamId)
                        .AddInputParam(SqlQueries.SelectStreamEvents.ParamFromVersion, DbType.Int32, fromVersion)
                        .AddInputParam(SqlQueries.SelectStreamEvents.ParamPessimisticLock, DbType.Boolean, pessimisticLock);

                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) // read snapshot
                        {
                            if (reader[0] == DBNull.Value) break;

                            // ReSharper disable once PossibleNullReferenceException
                            list.Add(await OnReadSnapshot(
                                streamId,
                                reader.GetString(0),
                                reader.GetInt32(1),
                                (byte[])reader[2]).ConfigureAwait(false));
                        }
                        await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);

                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) // read events
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            list.Add(await OnReadEvent(
                                streamId,
                                reader.GetString(0),
                                reader.GetInt32(1),
                                (byte[])reader[2]).ConfigureAwait(false));
                        }
                    }
                }
            }
            return list;
        }

        public async Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                var query = expectedVersion == ExpectedVersion.Any
                    ? SqlQueries.DeleteStream.QueryWhereExpectedVersionAny
                    : SqlQueries.DeleteStream.QueryWithExpectedVersion;

                using (var cmd = await cnn.OpenAndCreateCommandAsync(query, cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        await cmd
                            .AddInputParam(SqlQueries.DeleteStream.ParamStreamId, DbType.Guid, streamId)
                            .AddInputParam(SqlQueries.DeleteStream.ParamExpectedVersion, DbType.Int32, expectedVersion)
                            .ExecuteNonQueryAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (SqlException e)
                    {
                        if (e.Message.StartsWith("WrongExpectedVersion"))
                        {
                            throw new WrongExpectedVersionException($"Deleting stream {streamId} error", e);
                        }
                        throw;
                    }
                }
            }
        }

        public async Task AddSnapshot(Guid streamId, int version, string contractName, byte[] payload, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.InsertSnapshot.Query, cancellationToken).ConfigureAwait(false))
                    {
                        await cmd
                            .AddInputParam(SqlQueries.InsertSnapshot.ParamStreamId, DbType.Guid, streamId)
                            .AddInputParam(SqlQueries.InsertSnapshot.ParamVersion, DbType.Int32, version)
                            .AddInputParam(SqlQueries.InsertSnapshot.ParamContractName, DbType.AnsiString, contractName)
                            .AddInputParam(SqlQueries.InsertSnapshot.ParamGeneratedAtUtc, DbType.DateTime, DateTime.UtcNow)
                            .AddInputParam(SqlQueries.InsertSnapshot.ParamPayload, DbType.Binary, payload)
                            .ExecuteNonQueryAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (SqlException e)
                {
                    if (e.Number != duplicateKeyViolationErrorNumber) throw;
                    throw new SnapshotConcurrencyException(e, streamId, version, contractName);
                }
            }
        }

        public async Task SaveChanges(Guid streamId, Guid commitId, int expectedVersion, IEnumerable<EventRecord> events, byte[] metadata, CancellationToken cancellationToken = new CancellationToken())
        {
            var sqlEventRecords = SqlQueries.InsertEvents.CreateSqlDataRecords(events);

            using (var cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.InsertEvents.Query, cancellationToken).ConfigureAwait(false))
                    {
                        await cmd
                            .AddInputParam(SqlQueries.InsertEvents.ParamStreamId, DbType.Guid, streamId)
                            .AddInputParam(SqlQueries.InsertEvents.ParamCommitId, DbType.Guid, commitId)
                            .AddInputParam(SqlQueries.InsertEvents.ParamMetadataPayload, DbType.Binary, metadata)
                            .AddInputParam(SqlQueries.InsertEvents.ParamEvents, sqlEventRecords)
                            .ExecuteNonQueryAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (SqlException e)
                {
                    if (e.Number != duplicateKeyViolationErrorNumber) throw;
                    throw new StreamConcurrencyException(e, streamId, commitId, expectedVersion);
                }
            }
        }
    }
}

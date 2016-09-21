using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Ses.Abstracts;

namespace Ses.MsSql
{
    internal partial class MsSqlPersistor : IEventStreamPersistor
    {
        private readonly Linearizer _linearizer;
        private readonly string _connectionString;

        public MsSqlPersistor(Linearizer linearizer, string connectionString)
        {
            _linearizer = linearizer;
            _connectionString = connectionString;
        }

        public event OnReadEventHandler OnReadEvent;
        public event OnReadSnapshotHandler OnReadSnapshot;

        public IEvent[] Load(Guid streamId, int fromVersion, bool pessimisticLock)
        {
            List<IEvent> list = null;
            using (var cnn = new SqlConnection(_connectionString))
            {
                using (var cmd = cnn.OpenAndCreateCommand(SqlQueries.SelectEvents.Query))
                {
                    cmd
                        .AddInputParam(SqlQueries.SelectEvents.ParamStreamId, DbType.Guid, streamId)
                        .AddInputParam(SqlQueries.SelectEvents.ParamFromVersion, DbType.Int32, fromVersion)
                        .AddInputParam(SqlQueries.SelectEvents.ParamPessimisticLock, DbType.Boolean, pessimisticLock);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows) list = new List<IEvent>(100);

                        while (reader.Read()) // read snapshot
                        {
                            if (reader[0] == DBNull.Value) break;

                            // ReSharper disable once PossibleNullReferenceException
                            list.Add(OnReadSnapshot(
                                streamId,
                                reader.GetString(0),
                                reader.GetInt32(1),
                                (byte[])reader[2]));
                        }

                        reader.NextResult();

                        if (list == null && reader.HasRows) list = new List<IEvent>(30);

                        while (reader.Read()) // read events
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            list.Add(OnReadEvent(
                                streamId,
                                reader.GetString(0),
                                reader.GetInt32(1),
                                (byte[])reader[2]));
                        }
                    }
                }
            }
            return list?.ToArray() ?? new IEvent[0];
        }

        public void DeleteStream(Guid streamId, int expectedVersion)
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                var query = expectedVersion == ExpectedVersion.Any
                    ? SqlQueries.DeleteStream.QueryAny
                    : SqlQueries.DeleteStream.QueryExpectedVersion;

                using (var cmd = cnn.OpenAndCreateCommand(query))
                {
                    try
                    {
                        cmd.AddInputParam(SqlQueries.DeleteStream.ParamStreamId, DbType.Guid, streamId);
                        if (expectedVersion != ExpectedVersion.Any)
                            cmd.AddInputParam(SqlQueries.DeleteStream.ParamExpectedVersion, DbType.Int32, expectedVersion);

                        cmd.ExecuteNonQuery();
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

        public void UpdateSnapshot(Guid streamId, int version, string contractName, byte[] payload)
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = cnn.OpenAndCreateCommand(SqlQueries.UpdateSnapshot.Query))
                    {
                        cmd
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamStreamId, DbType.Guid, streamId)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamVersion, DbType.Int32, version)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamContractName, DbType.AnsiString, contractName)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamGeneratedAtUtc, DbType.DateTime, DateTime.UtcNow)
                            .AddInputParam(SqlQueries.UpdateSnapshot.ParamPayload, DbType.Binary, payload)
                            .ExecuteNonQuery();
                    }
                }
                catch (SqlException e)
                {
                    if (e.Message.StartsWith("WrongExpectedVersion"))
                    {
                        throw new WrongExpectedVersionException($"Updating snapshot for stream {streamId} error", e);
                    }
                    throw;
                }
            }
        }

        public void SaveChanges(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata, bool isLockable)
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                switch (expectedVersion)
                {
                    case ExpectedVersion.NoStream:
                        SaveChangesNoStream(cnn, events, streamId, commitId, metadata, isLockable);
                        break;
                    case ExpectedVersion.Any:
                        SaveChangesAny(cnn, events, streamId, commitId, metadata, isLockable);
                        break;
                    default:
                        SaveChangesExpectedVersion(cnn, events, streamId, commitId, expectedVersion, metadata);
                        break;
                }
            }

            _linearizer?.Start();
        }

        private static void SaveChangesExpectedVersion(SqlConnection cnn, EventRecord[] events, Guid streamId, Guid commitId, int expectedVersion, byte[] metadata)
        {
            try
            {
                using (var cmd = cnn.OpenAndCreateCommand(SqlQueries.InsertEvents.QueryExpectedVersion))
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
                        cmd.Parameters[5].Value = record.ContractName;
                        cmd.Parameters[6].Value = record.Version;
                        cmd.Parameters[7].Value = record.Payload;

                        cmd.ExecuteNonQuery();

                        cmd.Parameters[3].Value = DBNull.Value; // metadata is one per CommitId
                    }
                }
            }
            catch (SqlException e)
            {
                if (e.IsUniqueConstraintViolation() || e.IsWrongExpectedVersionRised())
                {
                    throw new WrongExpectedVersionException($"Saving new or existing stream {streamId} error. Stream exists.", e);
                }
                throw;
            }
        }

        private static void SaveChangesAny(SqlConnection cnn, EventRecord[] events, Guid streamId, Guid commitId, byte[] metadata, bool isLockable)
        {
            try
            {
                using (var cmd = cnn.OpenAndCreateCommand(SqlQueries.InsertEvents.QueryAny))
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
                        cmd.Parameters[5].Value = record.ContractName;
                        cmd.Parameters[6].Value = record.Version;
                        cmd.Parameters[7].Value = record.Payload;

                        cmd.ExecuteNonQuery();

                        cmd.Parameters[3].Value = DBNull.Value; // metadata is one per CommitId
                    }
                }
            }
            catch (SqlException e)
            {
                if (e.IsUniqueConstraintViolation() || e.IsWrongExpectedVersionRised())
                {
                    throw new WrongExpectedVersionException($"Saving new or existing stream {streamId} error. Stream exists.", e);
                }
                throw;
            }
        }

        private static void SaveChangesNoStream(SqlConnection cnn, EventRecord[] events, Guid streamId, Guid commitId, byte[] metadata, bool isLockable)
        {
            try
            {
                using (var cmd = cnn.OpenAndCreateCommand(SqlQueries.InsertEvents.QueryNoStream))
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
                        cmd.Parameters[5].Value = record.ContractName;
                        cmd.Parameters[6].Value = record.Version;
                        cmd.Parameters[7].Value = record.Payload;

                        cmd.ExecuteNonQuery();

                        cmd.Parameters[3].Value = DBNull.Value; // metadata is one per CommitId
                    }
                }
            }
            catch (SqlException e)
            {
                if (e.IsUniqueConstraintViolation() || e.IsWrongExpectedVersionRised())
                {
                    throw new WrongExpectedVersionException($"Saving new stream {streamId} error. Stream exists.", e);
                }
                throw;
            }
        }

        public void Dispose()
        {
            _linearizer?.Dispose();
        }
    }
}

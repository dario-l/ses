using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts.Extensions;

namespace Ses.Subscriptions.MsSql
{
    public class MsSqlPollerStateRepository : IPollerStateRepository
    {
        private const byte colIndexForPollerContractName = 0;
        private const byte colIndexForSourceContractName = 1;
        private const byte colIndexForHandlerContractName = 2;
        private const byte colIndexForEventSequence = 3;

        private readonly string _connectionString;

        public MsSqlPollerStateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MsSqlPollerStateRepository Initialize()
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = Scripts.Ses_Subscriptions_Initialize;
                cnn.Open();
                cmd.ExecuteNonQuery();
            }
            return this;
        }

        public MsSqlPollerStateRepository Destroy(bool ignoreErrors = false)
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                try
                {
                    cmd.CommandText = Scripts.Ses_Subscriptions_Destroy;
                    cnn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    if (!ignoreErrors) throw;
                }
            }
            return this;
        }

        public async Task<PollerState[]> LoadAsync(string pollerContractName, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<PollerState> states = null;
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.SelectStates;
                cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, pollerContractName);
                await cmd.Connection.OpenAsync(cancellationToken).NotOnCapturedContext();
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken).NotOnCapturedContext())
                {
                    if (reader.HasRows)
                    {
                        states = new List<PollerState>(100);
                        while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext())
                        {
                            var state = new PollerState(
                                await reader.GetFieldValueAsync<string>(colIndexForPollerContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<string>(colIndexForSourceContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<string>(colIndexForHandlerContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<long>(colIndexForEventSequence, cancellationToken).NotOnCapturedContext());
                            states.Add(state);
                        }
                    }
                }
            }
            return states?.ToArray() ?? new PollerState[0];
        }

        public async Task CreateStatesAsync(PollerState[] states, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (states.Length == 0) return;
            using (var cnn = new SqlConnection(_connectionString))
            {
                await cnn.OpenAsync(cancellationToken);
                foreach (var state in states)
                {
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = SqlClientScripts.InsertState;
                        cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, state.PollerContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamEventSequence, DbType.Int64, 0);
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
        }

        public async Task DeleteStatesAsync(PollerState[] states, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (states.Length == 0) return;
            using (var cnn = new SqlConnection(_connectionString))
            {
                await cnn.OpenAsync(cancellationToken);
                foreach (var state in states)
                {
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = SqlClientScripts.DeleteState;
                        cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, state.PollerContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
        }

        public async Task InsertOrUpdateAsync(PollerState state, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                await cnn.OpenAsync(cancellationToken).NotOnCapturedContext();
                cmd.CommandText = SqlClientScripts.UpdateState;
                cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, state.PollerContractName);
                cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                cmd.AddInputParam(SqlClientScripts.ParamEventSequence, DbType.Int64, state.EventSequenceId);
                if (await cmd.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext() == 0)
                {
                    cmd.CommandText = SqlClientScripts.InsertState;
                    await cmd.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                }
            }
        }

        public PollerState[] Load(string pollerContractName)
        {
            List<PollerState> states = null;
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.SelectStates;
                cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, pollerContractName);
                cmd.Connection.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess))
                {
                    if (reader.HasRows)
                    {
                        states = new List<PollerState>(100);
                        while (reader.Read())
                        {
                            var state = new PollerState(
                                reader.GetFieldValue<string>(colIndexForPollerContractName),
                                reader.GetFieldValue<string>(colIndexForSourceContractName),
                                reader.GetFieldValue<string>(colIndexForHandlerContractName),
                                reader.GetFieldValue<long>(colIndexForEventSequence));

                            states.Add(state);
                        }
                    }
                }
            }
            return states?.ToArray() ?? new PollerState[0];
        }

        public void CreateStates(PollerState[] states)
        {
            if (states.Length == 0) return;
            using (var cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                foreach (var state in states)
                {
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = SqlClientScripts.InsertState;
                        cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, state.PollerContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamEventSequence, DbType.Int64, 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteStates(PollerState[] states)
        {
            if (states.Length == 0) return;
            using (var cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                foreach (var state in states)
                {
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = SqlClientScripts.DeleteState;
                        cmd.AddInputParam(SqlClientScripts.ParamPollerContractName, DbType.String, state.PollerContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                        cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
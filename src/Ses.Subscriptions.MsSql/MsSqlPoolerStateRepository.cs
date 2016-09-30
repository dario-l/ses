using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts.Extensions;

namespace Ses.Subscriptions.MsSql
{
    public class MsSqlPoolerStateRepository : IPoolerStateRepository
    {
        private const byte colIndexForPoolerContractName = 0;
        private const byte colIndexForSourceContractName = 1;
        private const byte colIndexForHandlerContractName = 2;
        private const byte colIndexForEventSequence = 3;

        private readonly string _connectionString;

        public MsSqlPoolerStateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MsSqlPoolerStateRepository Initialize()
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

        public MsSqlPoolerStateRepository Destroy(bool ignoreErrors = false)
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

        public async Task<PoolerState[]> LoadAsync(string poolerContractName, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<PoolerState> states = null;
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.SelectStates;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, poolerContractName);
                await cmd.Connection.OpenAsync(cancellationToken).NotOnCapturedContext();
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken).NotOnCapturedContext())
                {
                    if (reader.HasRows)
                    {
                        states = new List<PoolerState>(100);
                        while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext())
                        {
                            var state = new PoolerState(
                                await reader.GetFieldValueAsync<string>(colIndexForPoolerContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<string>(colIndexForSourceContractName, cancellationToken).NotOnCapturedContext(),
                                await reader.GetFieldValueAsync<string>(colIndexForHandlerContractName, cancellationToken).NotOnCapturedContext())
                            {
                                EventSequenceId = await reader.GetFieldValueAsync<long>(colIndexForEventSequence, cancellationToken).NotOnCapturedContext()
                            };
                            states.Add(state);
                        }
                    }
                }
            }
            return states?.ToArray() ?? new PoolerState[0];
        }

        public async Task InsertOrUpdateAsync(PoolerState state, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                await cnn.OpenAsync(cancellationToken).NotOnCapturedContext();
                cmd.CommandText = SqlClientScripts.UpdateState;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, state.PoolerContractName);
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

        public async Task RemoveNotUsedStatesAsync(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (handlerContractNames.Length == 0 || sourceContractNames.Length == 0) return;

            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.DeleteNotUsedStates;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, poolerContractName);
                cmd.AddArrayParameters(SqlClientScripts.ParamHandlerContractNames, DbType.String, handlerContractNames);
                cmd.AddArrayParameters(SqlClientScripts.ParamSourceContractNames, DbType.String, sourceContractNames);
                await cnn.OpenAsync(cancellationToken).NotOnCapturedContext();
                await cmd.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
            }
        }

        public void RemoveNotUsedStates(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames)
        {
            if (handlerContractNames.Length == 0 || sourceContractNames.Length == 0) return;

            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.DeleteNotUsedStates;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, poolerContractName);
                cmd.AddArrayParameters(SqlClientScripts.ParamHandlerContractNames, DbType.String, handlerContractNames);
                cmd.AddArrayParameters(SqlClientScripts.ParamSourceContractNames, DbType.String, sourceContractNames);
                cnn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
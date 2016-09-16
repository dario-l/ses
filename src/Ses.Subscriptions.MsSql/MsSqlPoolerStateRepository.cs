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

        public async Task<IList<PoolerState>> LoadAsync(string poolerContractName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var states = new List<PoolerState>(100);
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.SelectStates;
                await cmd.Connection.OpenAsync(cancellationToken).NotOnCapturedContext();
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken).NotOnCapturedContext())
                {
                    while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext())
                    {
                        var state = new PoolerState(reader.GetString(0), reader.GetString(1), reader.GetString(2))
                        {
                            EventSequenceId = reader.GetInt64(3)
                        };
                        states.Add(state);
                    }
                }
            }
            return states;
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
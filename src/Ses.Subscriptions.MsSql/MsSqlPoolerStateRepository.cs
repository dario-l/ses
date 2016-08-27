using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

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
                cmd.CommandText = SqlClientScripts.Initialize;
                cnn.Open();
                cmd.ExecuteNonQuery();
            }
            return this;
        }

        public async Task<IList<PoolerState>> LoadAll()
        {
            var states = new List<PoolerState>(50); // TODO: states should be cached
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.SelectStates;
                await cmd.Connection.OpenAsync().NotOnCapturedContext();
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess).NotOnCapturedContext())
                {
                    while (await reader.ReadAsync().NotOnCapturedContext())
                    {
                        var state = new PoolerState((string)reader[0], (string)reader[1], (string)reader[2]) { EventSequenceId = (long)reader[3] };
                        states.Add(state);
                    }
                }
            }

            return states;
        }

        public async Task InsertOrUpdate(PoolerState state)
        {
            Debug.WriteLine("InsertOrUpdate: " + state);
            // TODO: update state in the cache
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                await cnn.OpenAsync().NotOnCapturedContext();
                cmd.CommandText = SqlClientScripts.UpdateState;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, state.PoolerContractName);
                cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                cmd.AddInputParam(SqlClientScripts.ParamEventSequence, DbType.Int64, state.EventSequenceId);
                if (await cmd.ExecuteNonQueryAsync().NotOnCapturedContext() != 0) return;
                cmd.CommandText = SqlClientScripts.InsertState;
                await cmd.ExecuteNonQueryAsync().NotOnCapturedContext();
            }
        }

        public async Task RemoveNotUsedStates(string poolerContractName, IEnumerable<string> handlerContractNames, IEnumerable<string> sourceContractNames)
        {
            // TODO: empty state cache
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = SqlClientScripts.DeleteNotUsedStates;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, poolerContractName);
                cmd.AddArrayParameters(SqlClientScripts.ParamHandlerContractNames, DbType.String, handlerContractNames);
                cmd.AddArrayParameters(SqlClientScripts.ParamSourceContractNames, DbType.String, sourceContractNames);
                await cnn.OpenAsync().NotOnCapturedContext();
                await cmd.ExecuteNonQueryAsync().NotOnCapturedContext();
            }
        }
    }
}
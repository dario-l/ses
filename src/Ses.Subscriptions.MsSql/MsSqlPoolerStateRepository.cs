using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Ses.Abstracts.Extensions;

namespace Ses.Subscriptions.MsSql
{
    public class MsSqlPoolerStateRepository : IPoolerStateRepository
    {
        private static readonly object locker = new object();
        private readonly string _connectionString;
        private volatile ConcurrentBag<PoolerState> _states;

        public MsSqlPoolerStateRepository(string connectionString)
        {
            _connectionString = connectionString;
            _states = new ConcurrentBag<PoolerState>();
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

        public MsSqlPoolerStateRepository Destroy(bool ignoreErrors = false)
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                try
                {
                    cmd.CommandText = SqlClientScripts.Destroy;
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

        public async Task<IReadOnlyList<PoolerState>> LoadAll()
        {
            if (_states.Count > 0) return _states.ToList();

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
                        _states.Add(state);
                    }
                }
            }

            return _states.ToList();
        }

        public async Task InsertOrUpdate(PoolerState state)
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                await cnn.OpenAsync().NotOnCapturedContext();
                cmd.CommandText = SqlClientScripts.UpdateState;
                cmd.AddInputParam(SqlClientScripts.ParamPoolerContractName, DbType.String, state.PoolerContractName);
                cmd.AddInputParam(SqlClientScripts.ParamSourceContractName, DbType.String, state.SourceContractName);
                cmd.AddInputParam(SqlClientScripts.ParamHandlerContractName, DbType.String, state.HandlerContractName);
                cmd.AddInputParam(SqlClientScripts.ParamEventSequence, DbType.Int64, state.EventSequenceId);
                if (await cmd.ExecuteNonQueryAsync().NotOnCapturedContext() == 0)
                {
                    cmd.CommandText = SqlClientScripts.InsertState;
                    await cmd.ExecuteNonQueryAsync().NotOnCapturedContext();

                    _states.Add(state);
                }
            }
        }

        public async Task RemoveNotUsedStates(string poolerContractName, IEnumerable<string> handlerContractNames, IEnumerable<string> sourceContractNames)
        {
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
            lock(locker)
            {
                while (!_states.IsEmpty)
                {
                    PoolerState someItem;
                    _states.TryTake(out someItem);
                }
            }
        }
    }
}
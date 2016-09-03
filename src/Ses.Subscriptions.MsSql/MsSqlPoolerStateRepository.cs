using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts.Extensions;

namespace Ses.Subscriptions.MsSql
{
    public class MsSqlPoolerStateRepository : IPoolerStateRepository
    {
        private readonly string _connectionString;
        private volatile ConcurrentBag<PoolerState> _states;
        private readonly SemaphoreSlim _mySemaphoreSlim = new SemaphoreSlim(1, 1);

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

        public async Task<IReadOnlyCollection<PoolerState>> Load(string poolerContractName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_states.IsEmpty) return _states.Where(x => x.PoolerContractName == poolerContractName).ToList();

            await _mySemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                using (var cnn = new SqlConnection(_connectionString))
                using (var cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = SqlClientScripts.SelectStates;
                    await cmd.Connection.OpenAsync(cancellationToken).NotOnCapturedContext();
                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken).NotOnCapturedContext())
                    {
                        while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext())
                        {
                            var state = new PoolerState((string)reader[0], (string)reader[1], (string)reader[2])
                            {
                                EventSequenceId = (long)reader[3]
                            };
                            _states.Add(state);
                        }
                    }
                }

                return _states.ToList();
            }
            finally
            {
                _mySemaphoreSlim.Release();
            }
        }

        public async Task InsertOrUpdate(PoolerState state, CancellationToken cancellationToken = default(CancellationToken))
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

                    _states.Add(state);
                }
            }
        }

        public async Task RemoveNotUsedStates(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (handlerContractNames.Length == 0 || sourceContractNames.Length == 0) return;

            await _mySemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                while (!_states.IsEmpty)
                {
                    PoolerState someItem;
                    _states.TryTake(out someItem);
                }

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
            finally
            {
                _mySemaphoreSlim.Release();
            }
        }
    }
}
using System;
using System.Data.SqlClient;
using System.Data.SqlLocalDb;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Conflicts;
using Ses.MsSql;

namespace Ses.TestBase
{
    public class DatabaseInstance : IDisposable
    {
        private readonly ISqlLocalDbInstance _localDbInstance;
        private readonly string _databaseName;
        private bool _databaseCreated;

        public DatabaseInstance(ISqlLocalDbInstance localDbInstance)
        {
            _localDbInstance = localDbInstance;
            _databaseName = "SesTests_" + Guid.NewGuid().ToString("N");
            ConnectionString = CreateConnectionString(_localDbInstance, _databaseName);
        }

        private static string CreateConnectionString(ISqlLocalDbInstance localDbInstance, string databaseName)
        {
            var connectionStringBuilder = localDbInstance.CreateConnectionStringBuilder();
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.InitialCatalog = databaseName;

            return connectionStringBuilder.ToString();
        }

        public string ConnectionString { get; }

        public async Task<IEventStore> GetEventStore(Assembly[] contractsRegistryAssemblies, IConcurrencyConflictResolver resolver = null)
        {
            if(!_databaseCreated) await CreateDatabase(GetLocation());

            var builder = new EventStoreBuilder()
                .WithDefaultContractsRegistry(contractsRegistryAssemblies)
                .WithMsSqlPersistor(ConnectionString, x =>
                {
                    x.Destroy(true);
                    x.Initialize();
                })
                .WithSerializer(new JilSerializer());

            if (resolver != null) builder.WithConcurrencyConflictResolver(resolver);

            return builder.Build();
        }

        private static string GetLocation()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        private async Task CreateDatabase(string location = null)
        {
            var commandText = location == null
                ? $"CREATE DATABASE {_databaseName}"
                : $"CREATE DATABASE {_databaseName} ON (name = '{_databaseName}', filename = '{Path.Combine(location, _databaseName)}')";

            using (var connection = _localDbInstance.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(commandText, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            _databaseCreated = true;
        }

        public void Dispose()
        {
            DropDatabase();
        }

        public void DropDatabase()
        {
            if (!_databaseCreated) return;

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                // Fixes: "Cannot drop database because it is currently in use"
                SqlConnection.ClearPool(sqlConnection);
            }

            using (var cnn = _localDbInstance.CreateConnection())
            {
                cnn.Open();
                using (var command = new SqlCommand($"DROP DATABASE {_databaseName}", cnn))
                {
                    command.ExecuteNonQuery();
                }
            }

            try
            {
                var path = GetLocation();
                foreach (var file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
                Directory.Delete(path, true);
            }
            catch
            {
                // nothing here
            }
            finally
            {
                _databaseCreated = false;
            }
        }
    }
}
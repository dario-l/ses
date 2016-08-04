using System;
using System.Data.SqlClient;
using System.Data.SqlLocalDb;
using System.IO;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.MsSql.Tests
{
    public abstract class TestsBase : IDisposable
    {
        private readonly ISqlLocalDbInstance _localDbInstance;
        private readonly string _databaseName;

        protected TestsBase()
        {
            var localDbProvider = new SqlLocalDbProvider();
            _localDbInstance = localDbProvider.GetOrCreateInstance("StreamStoreTests");
            _localDbInstance.Start();

            _databaseName = $"SesTests_{Guid.NewGuid().ToString("N")}";

            ConnectionString = CreateConnectionString();
        }

        protected string ConnectionString { get; private set; }

        protected async Task<IEventStore> GetEventStore()
        {
            await CreateDatabase(GetLocation());

            var store = new EventStoreBuilder()
                .WithDefaultContractsRegistry(typeof(TestsBase).Assembly)
                .WithMsSqlPersistor(ConnectionString, x =>
                {
                    x.Destroy(true);
                    x.Initialize();
                })
                .WithSerializer(new JilSerializer())
                .Build();

            return store;
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
        }

        private string CreateConnectionString()
        {
            var connectionStringBuilder = _localDbInstance.CreateConnectionStringBuilder();
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.InitialCatalog = _databaseName;

            return connectionStringBuilder.ToString();
        }

        public void Dispose()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                // Fixes: "Cannot drop database because it is currently in use"
                SqlConnection.ClearPool(sqlConnection);
            }

            using (var connection = _localDbInstance.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand($"DROP DATABASE {_databaseName}", connection))
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
                Directory.Delete(GetLocation(), true);
            }
            catch
            {
                // nothing here
            }
        }
    }
}
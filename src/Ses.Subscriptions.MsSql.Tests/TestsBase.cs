using System;
using System.Data.SqlClient;
using System.Data.SqlLocalDb;
using System.IO;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.MsSql;

namespace Ses.Subscriptions.MsSql.Tests
{
    public abstract class TestsBase : IDisposable
    {
        private readonly ISqlLocalDbInstance _localDbInstance;
        private string _databaseName;

        protected TestsBase()
        {
            var localDbProvider = new SqlLocalDbProvider();
            _localDbInstance = localDbProvider.GetOrCreateInstance("Ses.Subscriptions.MsSql.Tests");
            _localDbInstance.Start();
        }

        protected string ConnectionString { get; private set; }

        protected async Task<IEventStore> GetEventStore()
        {
            _databaseName = "SesTests_" + Guid.NewGuid().ToString("N");
            ConnectionString = CreateConnectionString(_localDbInstance, _databaseName);
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

        private static string CreateConnectionString(ISqlLocalDbInstance localDbInstance, string databaseName)
        {
            var connectionStringBuilder = localDbInstance.CreateConnectionStringBuilder();
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.InitialCatalog = databaseName;

            return connectionStringBuilder.ToString();
        }

        public void Dispose()
        {
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
        }
    }
}
using System;
using System.Data.SqlClient;

namespace Ses.MsSql
{
    internal class MsSqlPersistorBuilder : IMsSqlPersistorBuilder
    {
        private readonly string _connectionString;

        public MsSqlPersistorBuilder(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Destroy(bool ignoreErrors = false)
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.OpenAndCreateCommand(Scripts.Destroy))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    if (!ignoreErrors) throw;
                }
            }
        }

        public void Initialize()
        {
            using (var cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                var scripts = Scripts.Initialize.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var script in scripts)
                {
                    using (var cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = script;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
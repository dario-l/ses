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
            using (var cmd = cnn.OpenAndCreateCommand(Scripts.Initialize))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
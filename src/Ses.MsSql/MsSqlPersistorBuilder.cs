using System;
using System.Data.SqlClient;
using Ses.Abstracts;

namespace Ses.MsSql
{
    internal class MsSqlPersistorBuilder : IMsSqlPersistorBuilder
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;

        public MsSqlPersistorBuilder(ILogger logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
        }

        public void Destroy(bool ignoreErrors = false)
        {
            try
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
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
        }

        public void Initialize()
        {
            try
            {
                using (var cnn = new SqlConnection(_connectionString))
                {
                    cnn.Open();
                    var scripts = Scripts.Initialize.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var script in scripts)
                    {
                        using (var cmd = cnn.CreateCommand())
                        {
                            cmd.CommandText = script.Trim();
                            if(string.IsNullOrEmpty(cmd.CommandText)) continue;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
        }

        public Linearizer Linearizer { get; private set; }

        public void RunLinearizer(TimeSpan timeout)
        {
            Linearizer = new Linearizer(_logger, timeout, _connectionString);
            Linearizer.Start();
        }
    }
}
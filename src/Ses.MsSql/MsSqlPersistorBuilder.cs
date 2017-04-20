using System;
using System.Data.SqlClient;
using System.Diagnostics;
using Ses.Abstracts;

namespace Ses.MsSql
{
    internal class MsSqlPersistorBuilder : IMsSqlPersistorBuilder
    {
        private const byte defaultDurationWorkInSeconds = 60;
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
                using (var cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = Scripts.Ses_Destroy;
                    cnn.Open();

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch(Exception e)
                    {
                        _logger.Error(e.ToString());
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
                    var scripts = Scripts.Ses_Initialize.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
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
                Debug.WriteLine(e.ToString());
            }
        }

        public Linearizer Linearizer { get; private set; }

        public void RunLinearizer(TimeSpan timeout, TimeSpan? durationWork = null, int batchSize = 500)
        {
            Linearizer = new Linearizer(
                _connectionString,
                _logger,
                timeout,
                durationWork ?? TimeSpan.FromSeconds(defaultDurationWorkInSeconds),
                batchSize);
        }

        public void RunLinearizerNow()
        {
            Linearizer?.Start();
        }
    }
}
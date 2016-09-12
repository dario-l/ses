using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Extensions;

namespace Ses.MsSql
{
    internal class Linearizer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly System.Timers.Timer _timer;
        private readonly TimeSpan _durationWork;
        private volatile bool _isRunning;
        private readonly InterlockedDateTime _startedAt;

        public Linearizer(ILogger logger, TimeSpan timeout, TimeSpan durationWork, string connectionString)
        {
            _logger = logger;
            _connectionString = PrepareConnectionString(connectionString);
            _timer = new System.Timers.Timer(timeout.TotalMilliseconds) { AutoReset = false, SynchronizingObject = null,  Site = null };
            _timer.Elapsed += (_, __) => Execute().SwallowException();
            _durationWork = durationWork;
            _startedAt = new InterlockedDateTime(DateTime.MaxValue);
        }

        private static string PrepareConnectionString(string connectionString)
        {
            if (connectionString.ToLowerInvariant().Contains("enlist")) return connectionString;
            return connectionString.TrimEnd(';') + "; Enlist = false;";
        }


        public void Start()
        {
            _logger.Debug($"Starting linearizer for duration {_durationWork.TotalMinutes} minute(s)...");
            _startedAt.Set(DateTime.UtcNow);
            if (_isRunning) return;
            _isRunning = true;
            _timer.Start();
            _logger.Debug($"Linearizer for duration {_durationWork.TotalMinutes} minute(s) started.");
        }

        public void Stop()
        {
            if (_timer == null) return;
            _timer.Stop();
            _isRunning = false;
            _startedAt.Set(DateTime.MaxValue);
            _logger.Debug("Linearizer stopped.");
        }

        private async Task Execute()
        {
            if (ShouldStop())
            {
                Stop();
            }
            else
            {
                await Linearize();
            }
        }

        private bool ShouldStop()
        {
            return ((DateTime.UtcNow - _startedAt.Value) > _durationWork);
        }

        private async Task Linearize()
        {
            try
            {
                if (_connectionString == null) return;
                var cancellationToken = new CancellationToken();
                using (var cnn = new SqlConnection(_connectionString))
                using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.Linearize.Query, cancellationToken).NotOnCapturedContext())
                {
                    await cmd
                        .ExecuteNonQueryAsync(cancellationToken)
                        .NotOnCapturedContext();

                    cnn.Close();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
            finally
            {
                _timer.Start();
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }

        public bool IsRunning => _isRunning;
    }
}

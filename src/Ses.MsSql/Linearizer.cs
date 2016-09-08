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
            _connectionString = connectionString;
            _timer = new System.Timers.Timer(timeout.TotalMilliseconds) { AutoReset = false };
            _timer.Elapsed += (_, __) => Run().SwallowException();
            _durationWork = durationWork;
            _startedAt = new InterlockedDateTime(DateTime.MaxValue);
        }

        public void Start()
        {
            _startedAt.Set(DateTime.UtcNow);
            if (_isRunning) return;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer == null) return;
            _timer.Stop();
            _isRunning = false;
            _startedAt.Set(DateTime.MaxValue);
            _logger.Trace("Linealizer stopped.");
        }

        private async Task Run()
        {
            if (ShouldStop())
            {
                Stop();
            }
            else
            {
                await Linearize();
                _timer.Start();
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
                var cancellationToken = new CancellationToken();
                using (var cnn = new SqlConnection(_connectionString))
                {
                    using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.Linearize.Query, cancellationToken).NotOnCapturedContext())
                    {
                        await cmd
                            .ExecuteNonQueryAsync(cancellationToken)
                            .NotOnCapturedContext();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Ses.Abstracts;
using Ses.Abstracts.Extensions;

namespace Ses.MsSql
{
    internal class Linearizer : IDisposable
    {
        private const string batchSizeParamName = "@Limit";
        private CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private System.Timers.Timer _timer;
        private readonly TimeSpan _durationWork;
        private volatile bool _isRunning;
        private readonly InterlockedDateTime _startedAt;
        private readonly int _batchSize;

        public Linearizer(string connectionString, ILogger logger, TimeSpan timeout, TimeSpan durationWork, int batchSize)
        {
            _logger = logger;
            _connectionString = PrepareConnectionString(connectionString);
            _timer = new System.Timers.Timer(timeout.TotalMilliseconds) { AutoReset = false, SynchronizingObject = null,  Site = null };
            _timer.Elapsed += OnTimerElapsed;
            _durationWork = durationWork;
            _batchSize = batchSize;
            _startedAt = new InterlockedDateTime(DateTime.MaxValue);
        }

        private void OnTimerElapsed(object o, ElapsedEventArgs args)
        {
            Execute().SwallowException();
        }

        private static string PrepareConnectionString(string connectionString)
        {
            if (connectionString == null || connectionString.ToLowerInvariant().Contains("enlist")) return connectionString;
            return connectionString.TrimEnd(';') + "; Enlist = false;";
        }

        public void Start()
        {
            _startedAt.Set(DateTime.UtcNow);
            if (_isRunning) return;
            _isRunning = true;
            _timer.Start();
            _logger.Debug("Linearizer for duration {0} minute(s) started.", _durationWork.TotalMinutes);
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

        private bool ShouldStop() => (DateTime.UtcNow - _startedAt.Value) > _durationWork;

        private async Task Linearize()
        {
            try
            {
                if (_connectionString == null) return;
                using (var cnn = new SqlConnection(_connectionString))
                using (var cmd = await cnn.OpenAndCreateCommandAsync(SqlQueries.Linearize.Query, _disposedTokenSource.Token).NotOnCapturedContext())
                {
                    cmd.CommandTimeout = 60000;
                    cmd.AddInputParam(batchSizeParamName, DbType.Int32, _batchSize);
                    await cmd
                        .ExecuteNonQueryAsync(_disposedTokenSource.Token)
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_timer == null) return;
            if (disposing)
            {
                if (_timer != null)
                {
                    if (_timer.Enabled)
                    {
                        Stop();
                    }
                    _timer.Dispose();
                }
                _disposedTokenSource.Dispose();
            }
            _disposedTokenSource = null;
            _timer = null;
        }

        public bool IsRunning => _isRunning;
    }
}

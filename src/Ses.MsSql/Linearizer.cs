﻿using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.MsSql
{
    internal class Linearizer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly System.Timers.Timer _timer;
        private readonly TimeSpan _durationWork;
        private DateTime? _startedAt;
        private bool _isRunning;

        public Linearizer(ILogger logger, TimeSpan timeout, TimeSpan durationWork, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
            _timer = new System.Timers.Timer(timeout.TotalMilliseconds) { AutoReset = false };
            _timer.Elapsed += (_, __) => Run().SwallowException();
            _durationWork = durationWork;
        }

        public void Start()
        {
            if (_startedAt.HasValue) return;
            _startedAt = DateTime.UtcNow;
            _timer.Start();
        }

        private async Task Run()
        {
            if (_isRunning) return;
            _isRunning = true;
            if (ShouldStop())
            {
                _startedAt = null;
                _logger.Trace("Linealizer stopped.");
                return;
            }

            await Linearize();
            _isRunning = false;
            _timer.Start();
        }

        private bool ShouldStop()
        {
            return _startedAt.HasValue && ((DateTime.UtcNow - _startedAt.Value) > _durationWork);
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
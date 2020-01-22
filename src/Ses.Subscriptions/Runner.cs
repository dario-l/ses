using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Abstracts.Extensions;

namespace Ses.Subscriptions
{
    internal class Runner : IDisposable
    {
        public SubscriptionPoller Poller { get; }
        private System.Timers.Timer _runnerTimer;
        private CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        private volatile bool _isRunning;
        private volatile bool _isSlowedDownByPolicy;
        private readonly InterlockedDateTime _startedAt;

        private readonly PollerTimeoutCalculator _timeoutCalc;
        private readonly PollerContext _pollerContext;

        public Runner(IContractsRegistry contractsRegistry, ILogger logger, IPollerStateRepository stateRepository, SubscriptionPoller poller, IUpConverterFactory upConverterFactory)
        {
            Poller = poller ?? throw new ArgumentNullException(nameof(poller));

            _pollerContext = new PollerContext(contractsRegistry, logger, stateRepository, upConverterFactory);
            _startedAt = new InterlockedDateTime(DateTime.MaxValue);
            _timeoutCalc = new PollerTimeoutCalculator(Poller.GetFetchTimeout());
            _runnerTimer = CreateTimer(_timeoutCalc);
        }

        private System.Timers.Timer CreateTimer(PollerTimeoutCalculator timeoutCalc)
        {
            var timeout = timeoutCalc.CalculateNext();
            var result = new System.Timers.Timer(timeout) { AutoReset = false };
            result.Elapsed += OnTimerElapsed;
            return result;
        }

        private void OnTimerElapsed(object o, ElapsedEventArgs args)
        {
            Run().SwallowException();
        }

        public void ForceStart()
        {
            _isSlowedDownByPolicy = false;
            Start();
        }

        public void Start()
        {
            if (_isSlowedDownByPolicy)
            {
                _pollerContext.Logger.Warn($"Runner for poller {Poller.GetType().FullName} is slowed down. Use ForceStart.");
            }

            _pollerContext.Logger.Trace(
                Poller.RunForDuration != null
                    // ReSharper disable once PossibleInvalidOperationException
                    ? $"Starting runner for poller {Poller.GetType().FullName} for duration {Poller.RunForDuration.Value.TotalMinutes} minute(s)..."
                    : $"Starting runner for poller {Poller.GetType().FullName}...");
            _startedAt.Set(DateTime.UtcNow);
            if (_isRunning) return;
            _pollerContext.Logger.Debug(
                Poller.RunForDuration != null
                    // ReSharper disable once PossibleInvalidOperationException
                    ? $"Runner for poller {Poller.GetType().FullName} for duration {Poller.RunForDuration.Value.TotalMinutes} minute(s) started."
                    : $"Runner for poller {Poller.GetType().FullName} started.");
            _runnerTimer.Start();
            _isRunning = true;
        }

        private async Task Run()
        {
            if (ShouldStop())
            {
                Stop();
            }
            else
            {
                try
                {
                    var anyDispatched = await Poller.Execute(_pollerContext, _disposedTokenSource.Token);

                    if (anyDispatched)
                    {
                        // extend period of work duration
                        _startedAt.Set(DateTime.UtcNow);
                    }

                    _isSlowedDownByPolicy = false;
                    _runnerTimer.Interval = _timeoutCalc.CalculateNext(anyDispatched);
                    _runnerTimer.Start();
                }
                catch (Exception e)
                {
                    _isSlowedDownByPolicy = true;
                    _pollerContext.Logger.Error(e.ToString());
                    _runnerTimer.Interval = TimeSpan.FromSeconds(10).TotalMilliseconds;
                    _runnerTimer.Start();
                }
            }
        }

        private bool ShouldStop()
        {
            return Poller.RunForDuration.HasValue
                && ((DateTime.UtcNow - _startedAt.Value) > Poller.RunForDuration);
        }

        public void Stop()
        {
            if (_runnerTimer == null) return;
            _runnerTimer.Stop();
            _isRunning = false;
            _startedAt.Set(DateTime.MaxValue);
            _pollerContext.Logger.Debug($"Runner for poller {Poller.GetType().FullName} stopped.");
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_runnerTimer == null) return;
            if (disposing)
            {
                if (_runnerTimer != null)
                {
                    if (_runnerTimer.Enabled)
                    {
                        Stop();
                    }
                    _runnerTimer.Dispose();
                }
                _disposedTokenSource.Dispose();
            }
            _disposedTokenSource = null;
            _runnerTimer = null;
        }

        public RunnerInfo GetInfo()
        {
            return new RunnerInfo(_startedAt.Value, _isSlowedDownByPolicy, _isRunning);
        }

        public async Task StartOnce()
        {
            while (true)
            {
                var anyDispatched = await Poller.Execute(_pollerContext, _disposedTokenSource.Token);
                if (!anyDispatched) return;
            }
        }
    }
}

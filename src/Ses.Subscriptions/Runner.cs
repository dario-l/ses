using System;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Abstracts.Extensions;

namespace Ses.Subscriptions
{
    internal class Runner : IDisposable
    {
        public SubscriptionPooler Pooler { get; }
        private readonly System.Timers.Timer _runnerTimer;
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        private volatile bool _isRunning;
        private readonly InterlockedDateTime _startedAt;

        private readonly PoolerTimeoutCalculator _timeoutCalc;
        private readonly PoolerContext _poolerContext;

        public Runner(IContractsRegistry contractsRegistry, ILogger logger, IPoolerStateRepository stateRepository, SubscriptionPooler pooler, IUpConverterFactory upConverterFactory)
        {
            if (pooler == null) throw new ArgumentNullException(nameof(pooler));
            Pooler = pooler;

            _poolerContext = new PoolerContext(contractsRegistry, logger, stateRepository, upConverterFactory);
            _startedAt = new InterlockedDateTime(DateTime.MaxValue);
            _timeoutCalc = new PoolerTimeoutCalculator(Pooler.GetFetchTimeout());
            _runnerTimer = CreateTimer(_timeoutCalc);
        }

        private System.Timers.Timer CreateTimer(PoolerTimeoutCalculator timeoutCalc)
        {
            var timeout = timeoutCalc.CalculateNext(true);
            var result = new System.Timers.Timer(timeout) { AutoReset = false };
            result.Elapsed += (_, __) => Run().SwallowException();
            return result;
        }

        public void Start()
        {
            _poolerContext.Logger.Trace(Pooler.RunForDuration.HasValue
                // ReSharper disable once PossibleInvalidOperationException
                ? $"Starting runner for pooler {Pooler.GetType().FullName} for duration {Pooler.RunForDuration.Value.TotalMinutes} minute(s)..."
                : $"Starting runner for pooler {Pooler.GetType().FullName}...");
            _startedAt.Set(DateTime.UtcNow);
            if (_isRunning) return;
            _poolerContext.Logger.Debug(Pooler.RunForDuration.HasValue
                // ReSharper disable once PossibleInvalidOperationException
                ? $"Runner for pooler {Pooler.GetType().FullName} for duration {Pooler.RunForDuration.Value.TotalMinutes} minute(s) started."
                : $"Runner for pooler {Pooler.GetType().FullName} started.");
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
                var anyDispatched = await Pooler.Execute(_poolerContext, _disposedTokenSource.Token);
                _runnerTimer.Interval = _timeoutCalc.CalculateNext(anyDispatched);
                _runnerTimer.Start();
            }
        }

        private bool ShouldStop()
        {
            return Pooler.RunForDuration.HasValue
                && ((DateTime.UtcNow - _startedAt.Value) > Pooler.RunForDuration);
        }

        public void Stop()
        {
            if (_runnerTimer == null) return;
            _runnerTimer.Stop();
            _isRunning = false;
            _startedAt.Set(DateTime.MaxValue);
            _poolerContext.Logger.Debug($"Runner for pooler {Pooler.GetType().FullName} stopped.");
        }

        public void Dispose()
        {
            _disposedTokenSource.Cancel();
            _runnerTimer.Dispose();
        }
    }
}

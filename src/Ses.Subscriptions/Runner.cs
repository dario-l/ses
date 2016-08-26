using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;

namespace Ses.Subscriptions
{
    internal class Runner : IDisposable
    {
        public SubscriptionPooler Pooler { get; }
        private readonly IContractsRegistry _contractsRegistry;
        private readonly ILogger _logger;
        private readonly IPoolerStateRepository _poolerStateRepository;
        private readonly System.Timers.Timer _runnerTimer;
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        private volatile bool _isRunning;
        private short _notDispatchingCounter;
        private DateTime? _startedAt;

        private const short minTimeoutValue = 20;
        private const short maxTimeoutLevel = 10000;
        private static readonly Dictionary<short, short> timeoutLevels = new Dictionary<short, short>
        {
            // count | ms
            { maxTimeoutLevel, 1000 },
            { 5000, 500 },
            { 2000, 250 },
            { 1500, 100 },
            { 1000, 60 },
            { 500, 40 },
            { 0, minTimeoutValue }
        };

        public Runner(IContractsRegistry contractsRegistry, ILogger logger, IPoolerStateRepository poolerStateRepository, SubscriptionPooler pooler)
        {
            Pooler = pooler;
            _contractsRegistry = contractsRegistry;
            _logger = logger;
            _poolerStateRepository = poolerStateRepository;

            var timeout = Pooler.GetFetchTimeout() == TimeSpan.Zero ? minTimeoutValue : Pooler.GetFetchTimeout().TotalMilliseconds;
            _runnerTimer = new System.Timers.Timer(timeout) { AutoReset = false };
            _runnerTimer.Elapsed += (_, __) => Run().SwallowException();
        }

        public void Start()
        {
            if (_startedAt.HasValue) return;
            _logger.Debug(Pooler.RunForDuration.HasValue
                ? $"Starting runner for pooler {Pooler.GetType().FullName} for duration {Pooler.RunForDuration.Value.TotalMinutes} minute(s)..."
                : $"Starting runner for pooler {Pooler.GetType().FullName}...");
            _runnerTimer.Start();
            _startedAt = DateTime.UtcNow;
        }

        private async Task Run()
        {
            if (_isRunning) return;
            _isRunning = true;

            if (ShouldStop())
            {
                Stop();
                _isRunning = false;
            }
            else
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId + " - " + Pooler.GetType().FullName + "...");
                var anyDispatched = await Pooler.Execute(_contractsRegistry, _poolerStateRepository, _logger, _disposedTokenSource.Token);
                _runnerTimer.Interval = CalculateTimeout(anyDispatched, ref _notDispatchingCounter);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId + " - " + Pooler.GetType().FullName + " - " + _runnerTimer.Interval + "ms");
                _isRunning = false;
                _runnerTimer.Start();
            }
        }

        private bool ShouldStop()
        {
            return Pooler.RunForDuration.HasValue
                && _startedAt.HasValue
                && ((DateTime.UtcNow - _startedAt.Value) > Pooler.RunForDuration.Value);
        }

        private double CalculateTimeout(bool anyDispatched, ref short notDispatchingCounter)
        {
            var fetchTimeout = Pooler.GetFetchTimeout();
            if (fetchTimeout != TimeSpan.Zero)
            {
                notDispatchingCounter = 0;
                return anyDispatched ? minTimeoutValue : fetchTimeout.TotalMilliseconds;
            }

            if (!anyDispatched)
            {
                if (notDispatchingCounter >= maxTimeoutLevel) return timeoutLevels[maxTimeoutLevel];
                var levelValue = minTimeoutValue;
                foreach (var level in timeoutLevels)
                {
                    if (notDispatchingCounter <= level.Key) continue;
                    levelValue = level.Value;
                    break;
                }
                notDispatchingCounter++;
                return levelValue;
            }
            notDispatchingCounter = 0;
            return minTimeoutValue;
        }

        public void Stop()
        {
            _startedAt = null;
            if (_runnerTimer == null) return;
            _runnerTimer.Stop();
            _logger.Debug($"Stopping runner for pooler {Pooler.GetType().FullName}...");
        }

        public void Dispose()
        {
            _disposedTokenSource.Cancel();
            _runnerTimer.Dispose();
        }
    }
}

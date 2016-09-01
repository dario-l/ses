using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Logging;

namespace Ses.Subscriptions
{
    public class EventStoreSubscriptions : IDisposable
    {
        private readonly IDictionary<Type, Runner> _runners;
        private readonly IList<SubscriptionPooler> _poolers;
        private readonly IPoolerStateRepository _poolerStateRepository;
        private IContractsRegistry _contractRegistry;
        private ILogger _logger;

        public EventStoreSubscriptions(IPoolerStateRepository poolerStateRepository)
        {
            _poolerStateRepository = poolerStateRepository;
            _poolers = new List<SubscriptionPooler>();
            _runners = new Dictionary<Type, Runner>();
            _logger = new NullLogger();
        }

        public EventStoreSubscriptions Add(SubscriptionPooler pooler)
        {
            if (_poolers.Contains(pooler)) return this;
            _poolers.Add(pooler);
            return this;
        }

        public EventStoreSubscriptions WithDefaultContractsRegistry(params Assembly[] assemblies)
        {
            return WithContractsRegistry(new DefaultContractsRegistry(assemblies));
        }

        public EventStoreSubscriptions WithContractsRegistry(IContractsRegistry registry)
        {
            _contractRegistry = registry;
            return this;
        }

        public EventStoreSubscriptions WithLogger(DebugLogger logger)
        {
            _logger = logger;
            return this;
        }

        public async Task<EventStoreSubscriptions> Start()
        {
            if (_contractRegistry == null) throw new InvalidOperationException("Contract registry is not set. Use own IContractRegistry implementation or DefaultContractsRegistry.");

            foreach (var pooler in _poolers)
            {
                await ClearUnusedStates(pooler);
                await pooler.OnStart(_contractRegistry);

                var runner = new Runner(_contractRegistry, _logger, _poolerStateRepository, pooler);
                _runners.Add(pooler.GetType(), runner);
            }

            foreach (var runner in _runners.Values)
            {
                runner.Start();
            }
            return this;
        }

        private async Task ClearUnusedStates(SubscriptionPooler pooler)
        {
            var poolerContractName = _contractRegistry.GetContractName(pooler.GetType());
            var handlerTypes = pooler.GetRegisteredHanlders();
            var sourceTypes = pooler.Sources.Select(x => x.GetType()).ToList();

            await _poolerStateRepository.RemoveNotUsedStates(
                poolerContractName,
                handlerTypes.Select(x => _contractRegistry.GetContractName(x)).ToList(),
                sourceTypes.Select(x => _contractRegistry.GetContractName(x)).ToList());
        }

        public void Dispose()
        {
            if (_runners == null) return;
            foreach (var runner in _runners.Values)
            {
                runner.Stop();
                runner.Dispose();
            }
        }

        public void RunPooler(Type type)
        {
            if (!_runners.ContainsKey(type)) throw new InvalidOperationException($"Pooler {type.FullName} is not registered.");
            _runners[type].Start();
        }
    }
}
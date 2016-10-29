using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Abstracts.Logging;

namespace Ses.Subscriptions
{
    public class EventStoreSubscriptions : IEventStoreSubscriptions
    {
        private readonly Dictionary<Type, Runner> _runners;
        private readonly List<SubscriptionPoller> _pollers;
        private readonly IPollerStateRepository _pollerStateRepository;
        private IContractsRegistry _contractRegistry;
        private ILogger _logger;
        private IUpConverterFactory _upConverterFactory;

        public EventStoreSubscriptions(IPollerStateRepository pollerStateRepository)
        {
            _pollerStateRepository = pollerStateRepository;
            _pollers = new List<SubscriptionPoller>();
            _runners = new Dictionary<Type, Runner>();
            _logger = new NullLogger();
        }

        public Type[] GetPollerTypes()
        {
            return _runners.Keys.ToArray();
        }

        public EventStoreSubscriptions Add(SubscriptionPoller poller)
        {
            if (_pollers.Contains(poller)) return this;
            _pollers.Add(poller);
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

        public EventStoreSubscriptions WithLogger(ILogger logger)
        {
            _logger = logger;
            return this;
        }

        public EventStoreSubscriptions WithUpConverterFactory(IUpConverterFactory factory)
        {
            _upConverterFactory = factory;
            return this;
        }

        public EventStoreSubscriptions Start()
        {
            if (_contractRegistry == null)
                throw new InvalidOperationException("Contract registry is not set. Use own IContractRegistry implementation or DefaultContractsRegistry.");

            foreach (var poller in _pollers)
            {
                ClearUnusedStates(poller);
                poller.Initialize(_contractRegistry);

                var runner = new Runner(_contractRegistry, _logger, _pollerStateRepository, poller, _upConverterFactory);
                _runners.Add(poller.GetType(), runner);
                runner.Start();
            }
            return this;
        }

        public async Task<EventStoreSubscriptions> StartAsync()
        {
            if (_contractRegistry == null)
                throw new InvalidOperationException("Contract registry is not set. Use own IContractRegistry implementation or DefaultContractsRegistry.");

            foreach (var poller in _pollers)
            {
                await ClearUnusedStatesAsync(poller);
                await poller.InitializeAsync(_contractRegistry);

                var runner = new Runner(_contractRegistry, _logger, _pollerStateRepository, poller, _upConverterFactory);
                _runners.Add(poller.GetType(), runner);
                runner.Start();
            }
            return this;
        }

        private void ClearUnusedStates(SubscriptionPoller poller)
        {
            var pollerContractName = _contractRegistry.GetContractName(poller.GetType());
            var handlerTypes = poller.GetRegisteredHandlers();
            var sourceTypes = poller.Sources.Select(x => x.GetType()).ToList();

            _pollerStateRepository.RemoveNotUsedStates(
                pollerContractName,
                handlerTypes.Select(x => _contractRegistry.GetContractName(x)).ToArray(),
                sourceTypes.Select(x => _contractRegistry.GetContractName(x)).ToArray());
        }

        private async Task ClearUnusedStatesAsync(SubscriptionPoller poller)
        {
            var pollerContractName = _contractRegistry.GetContractName(poller.GetType());
            var handlerTypes = poller.GetRegisteredHandlers();
            var sourceTypes = poller.Sources.Select(x => x.GetType()).ToList();

            await _pollerStateRepository.RemoveNotUsedStatesAsync(
                pollerContractName,
                handlerTypes.Select(x => _contractRegistry.GetContractName(x)).ToArray(),
                sourceTypes.Select(x => _contractRegistry.GetContractName(x)).ToArray());
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

        public void RunStoppedPoller(Type type, bool force = false)
        {
            if (!_runners.ContainsKey(type)) throw new InvalidOperationException($"Poller {type.FullName} is not registered.");
            if (force)
            {
                _runners[type].ForceStart();
            }
            else
            {
                _runners[type].Start();
            }
        }

        public void RunStoppedPollers()
        {
            foreach (var runner in _runners)
            {
                runner.Value.Start();
            }
        }
    }
}
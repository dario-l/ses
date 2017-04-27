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
                SynchronizeStates(poller);
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
                await SynchronizeStatesAsync(poller);
                await poller.InitializeAsync(_contractRegistry);

                var runner = new Runner(_contractRegistry, _logger, _pollerStateRepository, poller, _upConverterFactory);
                _runners.Add(poller.GetType(), runner);
                runner.Start();
            }
            return this;
        }

        private void SynchronizeStates(SubscriptionPoller poller)
        {
            var pollerContractName = _contractRegistry.GetContractName(poller.GetType());
            var handlerContractNames = poller.GetRegisteredHandlers().Select(x => _contractRegistry.GetContractName(x)).ToArray();
            var sourceContractNames = poller.Sources.Select(x => _contractRegistry.GetContractName(x.GetType())).ToArray();

            var storedStates = _pollerStateRepository.Load(pollerContractName);

            var statesToDelete = new List<PollerState>(5);
            var statesToAdd = new List<PollerState>(5);

            FillStatesToSynchronize(
                pollerContractName,
                storedStates,
                sourceContractNames,
                handlerContractNames,
                statesToAdd,
                statesToDelete);

            _pollerStateRepository.DeleteStates(statesToDelete.ToArray());
            _pollerStateRepository.CreateStates(statesToAdd.ToArray());
        }

        private async Task SynchronizeStatesAsync(SubscriptionPoller poller)
        {
            var pollerContractName = _contractRegistry.GetContractName(poller.GetType());
            var handlerContractNames = poller.GetRegisteredHandlers().Select(x => _contractRegistry.GetContractName(x)).ToArray();
            var sourceContractNames = poller.Sources.Select(x => _contractRegistry.GetContractName(x.GetType())).ToArray();

            var storedStates = await _pollerStateRepository.LoadAsync(pollerContractName);

            var statesToDelete = new List<PollerState>(5);
            var statesToAdd = new List<PollerState>(5);

            FillStatesToSynchronize(
                pollerContractName,
                storedStates,
                sourceContractNames,
                handlerContractNames,
                statesToAdd,
                statesToDelete);

            await _pollerStateRepository.DeleteStatesAsync(statesToDelete.ToArray());
            await _pollerStateRepository.CreateStatesAsync(statesToAdd.ToArray());
        }

        private static void FillStatesToSynchronize(string pollerContractName, PollerState[] storedStates, string[] sourceContractNames, string[] handlerContractNames, List<PollerState> statesToAdd, List<PollerState> statesToDelete)
        {
            foreach (var state in storedStates)
            {
                if (!sourceContractNames.Contains(state.SourceContractName))
                {
                    statesToDelete.Add(state);
                }
                else if (!handlerContractNames.Contains(state.HandlerContractName))
                {
                    statesToDelete.Add(state);
                }
            }

            foreach (var source in sourceContractNames)
            {
                foreach (var handler in handlerContractNames)
                {
                    if (storedStates.FirstOrDefault(x => x.SourceContractName == source && x.HandlerContractName == handler) == null)
                    {
                        statesToAdd.Add(new PollerState(
                            pollerContractName,
                            source,
                            handler));
                    }
                }
            }
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

        public PollerInfo[] GetPollers()
        {
            return _pollers
                .Select(x => x.GetInfo(
                    _runners[x.GetType()].GetInfo(),
                    _contractRegistry,
                    _pollerStateRepository))
                .ToArray();
        }

        public async Task<EventStoreSubscriptions> StartAndWaitForStoppingByAsync()
        {
            if (_contractRegistry == null)
                throw new InvalidOperationException("Contract registry is not set. Use own IContractRegistry implementation or DefaultContractsRegistry.");

            foreach (var poller in _pollers)
            {
                SynchronizeStates(poller);
                poller.Initialize(_contractRegistry);

                var runner = new Runner(_contractRegistry, _logger, _pollerStateRepository, poller, _upConverterFactory);
                _runners.Add(poller.GetType(), runner);
                await runner.StartOnce();
            }
            return this;
        }
    }
}
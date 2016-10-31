using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions
{
    public abstract class SubscriptionPoller : ISubscriptionPoller
    {
        private readonly TransactionOptions _transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
        private readonly HandlerRegistrar _handlerRegistrar;
        private readonly Dictionary<ISubscriptionEventSource, int> _contractSubscriptions;
        private string _pollerContractName;

        protected SubscriptionPoller(ISubscriptionEventSource[] sources)
        {
            RetriesPolicy = PollerRetriesPolicy.Defaut();
            Sources = sources;
            _handlerRegistrar = CreateHandlerRegistrar();
            _contractSubscriptions = new Dictionary<ISubscriptionEventSource, int>(Sources.Length);
        }

        private HandlerRegistrar CreateHandlerRegistrar() { return new HandlerRegistrar(FindHandlerTypes()); }
        public ISubscriptionEventSource[] Sources { get; }
        public PollerRetriesPolicy RetriesPolicy { get; protected set; }
        public virtual TimeSpan? RunForDuration { get; } = null;
        public virtual TimeSpan GetFetchTimeout() { return TimeSpan.Zero; }

        protected abstract IEnumerable<Type> FindHandlerTypes();
        protected abstract IHandle CreateHandlerInstance(Type handlerType);
        protected virtual IEnumerable<Type> GetConcreteSubscriptionEventTypes() { return null; }
        protected virtual void PreHandleEvent(EventEnvelope envelope, Type handlerType) { }
        protected virtual void PostHandleEvent(EventEnvelope envelope, Type handlerType) { }
        protected virtual void PostHandleEventError(EventEnvelope envelope, Type handlerType, Exception exception, int retryAttempts) { }
        protected virtual void PreExecuting(int fetchedEventsCount) { }
        protected virtual void PostExecuting(int fetchedEventsCount, Type[] dispatchedHandlers) { }

        internal Type[] GetRegisteredHandlers()
        {
            return _handlerRegistrar.RegisteredHandlerTypes;
        }

        internal void Initialize(IContractsRegistry contractsRegistry)
        {
            if (RetriesPolicy == null) RetriesPolicy = PollerRetriesPolicy.NoRetries();
            _pollerContractName = contractsRegistry.GetContractName(GetType());
            var eventTypes = GetConcreteSubscriptionEventTypes();
            if (eventTypes == null) return;

            var contractNames = eventTypes.Select(contractsRegistry.GetContractName).ToArray();

            foreach (var source in Sources)
            {
                var id = source.CreateSubscriptionForContracts(_pollerContractName, contractNames);
                _contractSubscriptions.Add(source, id);
            }
        }

        internal async Task InitializeAsync(IContractsRegistry contractsRegistry)
        {
            _pollerContractName = contractsRegistry.GetContractName(GetType());
            var eventTypes = GetConcreteSubscriptionEventTypes();
            if (eventTypes == null) return;

            var contractNames = eventTypes.Select(contractsRegistry.GetContractName).ToArray();

            foreach (var source in Sources)
            {
                var id = await source.CreateSubscriptionForContractsAsync(_pollerContractName, contractNames);
                _contractSubscriptions.Add(source, id);
            }
        }

        internal async Task<bool> Execute(PollerContext ctx, CancellationToken cancellationToken = default(CancellationToken))
        {
            var anyDispatched = false;
            var executionRetryAttempts = 0;
            while (true)
            {
                try
                {
                    var pollerStates = new List<PollerState>(await ctx.StateRepository.LoadAsync(_pollerContractName, cancellationToken));
                    var timeline = await FetchEventTimeline(ctx, pollerStates);
                    PreExecuting(timeline.Count);

                    var typesListOfDispatchedHandlers = new List<Type>(_handlerRegistrar.RegisteredHandlerInfos.Length);
                    foreach (var item in timeline)
                    {
                        var sourceContractName = ctx.ContractsRegistry.GetContractName(item.SourceType);
                        foreach (var handlerInfo in _handlerRegistrar.RegisteredHandlerInfos) // all handlers can/should run in parallel
                        {
                            var state = FindOrCreateState(ctx.ContractsRegistry, pollerStates, sourceContractName, handlerInfo.HandlerType);
                            if (item.Envelope.SequenceId <= state.EventSequenceId) continue;

                            var handlingRetryAttempts = 0;
                            bool dispatched;
                            while (true)
                            {
                                try
                                {
                                    dispatched = await TryDispatch(ctx, handlerInfo, item.Envelope, state);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    PostHandleEventError(item.Envelope, handlerInfo.HandlerType, ex, handlingRetryAttempts);
                                    if (handlingRetryAttempts >= RetriesPolicy.HandlerAttemptsThreshold) throw;
                                    handlingRetryAttempts++;
                                }
                            }

                            if (dispatched && !typesListOfDispatchedHandlers.Contains(handlerInfo.HandlerType))
                                typesListOfDispatchedHandlers.Add(handlerInfo.HandlerType);

                            anyDispatched |= dispatched;
                        }
                    }

                    PostExecuting(timeline.Count, typesListOfDispatchedHandlers.ToArray());
                    return anyDispatched;
                }
                catch (Exception e)
                {
                    if (executionRetryAttempts >= RetriesPolicy.FetchAttemptsThreshold)
                        throw new FetchAttemptsThresholdException(GetType().Name, executionRetryAttempts, e);
                    executionRetryAttempts++;
                }
            }
        }

        private async Task<bool> TryDispatch(PollerContext ctx, HandlerRegistrar.HandlerTypeInfo handlerInfo, EventEnvelope envelope, PollerState state)
        {
            var eventType = envelope.Event.GetType();
            var shouldDispatch = handlerInfo.ContainsEventType(eventType);
            if (shouldDispatch) PreHandleEvent(envelope, handlerInfo.HandlerType);

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, _transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                if (shouldDispatch)
                {
                    var handlerInstance = CreateHandlerInstance(handlerInfo.HandlerType);
                    if (handlerInstance == null) throw new NullReferenceException($"Handler instance {handlerInfo.HandlerType.FullName} is null.");
                    ctx.Logger.Trace("Dispatching event {0} to {1}...", eventType.FullName, handlerInfo.HandlerType.FullName);
                    if (handlerInfo.IsAsync)
                    {
                        await ((dynamic)handlerInstance).Handle((dynamic)envelope.Event, envelope);
                    }
                    else
                    {
                        ((dynamic)handlerInstance).Handle((dynamic)envelope.Event, envelope);
                    }
                }
                state.EventSequenceId = envelope.SequenceId;
                await ctx.StateRepository.InsertOrUpdateAsync(state);
                scope.Complete();
            }
            if (shouldDispatch) PostHandleEvent(envelope, handlerInfo.HandlerType);
            return shouldDispatch;
        }

        private PollerState FindOrCreateState(IContractsRegistry contractsRegistry, List<PollerState> pollerStates, string sourceContractName, Type handlerType)
        {
            var handlerContractName = contractsRegistry.GetContractName(handlerType);

            PollerState state = null;
            foreach (var x in pollerStates)
            {
                if (x.HandlerContractName != handlerContractName || x.SourceContractName != sourceContractName) continue;
                state = x;
                break;
            }
            if (state != null) return state;
            state = new PollerState(_pollerContractName, sourceContractName, handlerContractName);
            pollerStates.Add(state);
            return state;
        }

        private async Task<List<ExtractedEvent>> FetchEventTimeline(PollerContext ctx, List<PollerState> pollerStates)
        {
            var tasks = new List<Task<List<ExtractedEvent>>>(Sources.Length);
            foreach (var source in Sources)
            {
                var minSequenceId = GetMinSequenceIdFor(ctx.ContractsRegistry, pollerStates, source);
                ctx.Logger.Trace("Min sequence id for {0} is {1}", _pollerContractName, minSequenceId.ToString());

                var concreteSubscriptionIdentifier = _contractSubscriptions.Count > 0 && _contractSubscriptions.ContainsKey(source)
                    ? _contractSubscriptions[source]
                    : (int?)null;

                tasks.Add(source.FetchAsync(
                    ctx.ContractsRegistry,
                    ctx.UpConverterFactory,
                    minSequenceId,
                    concreteSubscriptionIdentifier));
            }

            var events = await Task.WhenAll(tasks.ToArray());
            var merged = Merge(events);
            ctx.Logger.Trace("{0} fetched {1} events from {2} stream sources.", _pollerContractName, merged.Count.ToString(), Sources.Length.ToString());
            return merged;
        }

        private static List<ExtractedEvent> Merge(List<ExtractedEvent>[] list)
        {
            if (list.Length == 0) return new List<ExtractedEvent>(0);
            if (list.Length == 1) return list[0];

            var merged = list[0];
            for (var i = 1; i < list.Length; i++)
            {
                merged = merged
                    .MergeSorted(list[i], (l1, l2) => l1.Envelope.CreatedAtUtc > l2.Envelope.CreatedAtUtc ? 1 : -1)
                    .ToList();
            }

            return merged;
        }

        private static long GetMinSequenceIdFor(IContractsRegistry contractsRegistry, List<PollerState> pollerStates, ISubscriptionEventSource source)
        {
            var sourceContractName = contractsRegistry.GetContractName(source.GetType());
            long? min = null;
            foreach (var x in pollerStates)
            {
                if (x.SourceContractName != sourceContractName) continue;
                if (min == null || min > x.EventSequenceId) min = x.EventSequenceId;
            }
            return min ?? 0;
        }
    }
}
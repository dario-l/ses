using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.Domain
{
    public class Repository<TAggregate> : IRepository<TAggregate> where TAggregate : class, IAggregate, new()
    {
        private const string aggregateTypeClrMeta = "AggregateTypeClr";
        private readonly IEventStore _store;
        private const byte loadFromVersionDefault = 1;

        public Repository(IEventStore store)
        {
            _store = store;
        }

        public async Task<TAggregate> LoadAsync(Guid streamId, bool pessimisticLock = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var stream = await _store.LoadAsync(streamId, loadFromVersionDefault, pessimisticLock, cancellationToken);
            return stream == null ? null : RestoreAggregate(streamId, stream);
        }

        public TAggregate Load(Guid streamId, bool pessimisticLock = false)
        {
            var stream = _store.Load(streamId, loadFromVersionDefault, pessimisticLock);
            return stream == null ? null : RestoreAggregate(streamId, stream);
        }

        protected virtual TAggregate RestoreAggregate(Guid streamId, IReadOnlyEventStream stream)
        {
            var aggregate = new TAggregate();
            aggregate.Restore(streamId, stream.CommittedEvents);
            return aggregate;
        }

        public async Task SaveChangesAsync(TAggregate aggregate, Guid? commitId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            var events = aggregate.TakeUncommittedEvents();
            var stream = PrepareEventStreamToSave(commitId, events);
            OnBeforeSaveChanges(aggregate.Id, aggregate.CommittedVersion, stream);
            await _store.SaveChangesAsync(aggregate.Id, aggregate.CommittedVersion, stream, cancellationToken);
            OnAfterSaveChanges(aggregate.Id, aggregate.CommittedVersion, stream);
        }

        public void SaveChanges(TAggregate aggregate, Guid? commitId = null)
        {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            var events = aggregate.TakeUncommittedEvents();
            var stream = PrepareEventStreamToSave(commitId, events);
            OnBeforeSaveChanges(aggregate.Id, aggregate.CommittedVersion, stream);
            _store.SaveChanges(aggregate.Id, aggregate.CommittedVersion, stream);
            OnAfterSaveChanges(aggregate.Id, aggregate.CommittedVersion, stream);
        }

        protected virtual void OnAfterSaveChanges(Guid id, int committedVersion, IEventStream stream) { }

        protected virtual void OnBeforeSaveChanges(Guid id, int committedVersion, IEventStream stream) { }

        private IEventStream PrepareEventStreamToSave(Guid? commitId, IEvent[] events)
        {
            var stream = new EventStream(commitId ?? SequentialGuid.NewGuid(), events, IsAggregateLockable());
            CreateDefaultMetadata(stream);
            return stream;
        }

        protected virtual bool IsAggregateLockable() => false;

        protected virtual void CreateDefaultMetadata(IEventStream stream)
        {
            if (stream.Metadata == null) stream.Metadata = new Dictionary<string, object>(1);
            stream.Metadata.Add(aggregateTypeClrMeta, typeof(TAggregate).FullName);
        }

        public async Task DeleteAsync(Guid streamId, int expectedVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _store.Advanced.DeleteStreamAsync(streamId, expectedVersion, cancellationToken);
        }

        public void Delete(Guid streamId, int expectedVersion)
        {
            _store.Advanced.DeleteStream(streamId, expectedVersion);
        }
    }
}
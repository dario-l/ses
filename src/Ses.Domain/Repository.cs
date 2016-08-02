using System;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.Domain
{
    public class Repository<TAggregate> : IRepository<TAggregate> where TAggregate : class, IAggregate, new()
    {
        const string aggregateTypeClrMeta = "AggregateTypeClr";
        private readonly IEventStore _store;

        public Repository(IEventStore store)
        {
            _store = store;
        }

        public async Task<TAggregate> Load(Guid streamId, bool pessimisticLock = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var stream = await _store.Load(streamId, pessimisticLock, cancellationToken);
            if(stream == null) throw new AggregateNotFoundException(streamId, typeof(TAggregate));
            return RestoreAggregate(streamId, stream);
        }

        protected virtual TAggregate RestoreAggregate(Guid streamId, IReadOnlyEventStream stream)
        {
            var aggregate = new TAggregate();
            aggregate.Restore(streamId, stream.CommittedEvents);
            return aggregate;
        }

        public async Task SaveChanges(TAggregate aggregate, Guid? commitId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            var stream = new EventStream(commitId ?? SequentialGuid.NewGuid(), aggregate.TakeUncommittedEvents());
            PrepareEventStream(stream);
            var expectedVersion = aggregate.CommittedVersion + stream.Events.Count;
            await _store.SaveChanges(aggregate.Id, expectedVersion, stream, cancellationToken);
        }

        protected virtual void PrepareEventStream(EventStream stream)
        {
            stream.Metadata.Add(aggregateTypeClrMeta, typeof(TAggregate).FullName);
        }

        public async Task Delete(Guid streamId, int expectedVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _store.DeleteStream(streamId, expectedVersion, cancellationToken);
        }
    }
}
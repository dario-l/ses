using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.InMemory
{
    public class InMemoryPersistor : IEventStreamPersistor
    {
        public Task<IList<IEvent>> Load(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task AddSnapshot(Guid streamId, IMemento snapshot, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Func<Guid, string, byte[], IEvent> OnReadEvent { get; set; }
        public Func<Guid, string, byte[], IEvent> OnReadSnapshot { get; set; }
        public Task SaveChanges(Guid streamId, Guid commitId, int expectedVersion, IList<IEvent> events, IDictionary<string, object> metadata, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}

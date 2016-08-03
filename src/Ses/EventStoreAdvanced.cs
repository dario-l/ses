using System;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    internal class EventStoreAdvanced : IEventStoreAdvanced
    {
        private readonly IEventStoreSettings _settings;

        public EventStoreAdvanced(IEventStoreSettings settings)
        {
            _settings = settings;
        }

        public async Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            await _settings.Persistor.DeleteStream(streamId, expectedVersion, cancellationToken);
        }

        public async Task AddSnapshot(Guid streamId, int currentVersion, IMemento snapshot, CancellationToken cancellationToken = new CancellationToken())
        {
            var contractName = _settings.ContractsRegistry.GetContractName(snapshot.GetType());
            var payload = _settings.Serializer.Serialize(snapshot, snapshot.GetType());
            await _settings.Persistor.AddSnapshot(streamId, currentVersion, contractName, payload, cancellationToken);
        }
    }
}
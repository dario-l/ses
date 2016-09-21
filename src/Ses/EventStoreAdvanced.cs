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

        public async Task DeleteStreamAsync(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            if (expectedVersion == ExpectedVersion.NoStream)
                throw new InvalidOperationException($"Can not delete stream {streamId} when expecting no stream.");
            await _settings.Persistor.DeleteStreamAsync(streamId, expectedVersion, cancellationToken);
        }

        public async Task UpdateSnapshotAsync(Guid streamId, int currentVersion, IMemento snapshot, CancellationToken cancellationToken = new CancellationToken())
        {
            var contractName = _settings.ContractsRegistry.GetContractName(snapshot.GetType());
            var payload = _settings.Serializer.Serialize(snapshot, snapshot.GetType());
            await _settings.Persistor.UpdateSnapshotAsync(streamId, currentVersion, contractName, payload, cancellationToken);
        }

        public void DeleteStream(Guid streamId, int expectedVersion)
        {
            if (expectedVersion == ExpectedVersion.NoStream)
                throw new InvalidOperationException($"Can not delete stream {streamId} when expecting no stream.");
            _settings.Persistor.DeleteStream(streamId, expectedVersion);
        }

        public void UpdateSnapshot(Guid streamId, int currentVersion, IMemento snapshot)
        {
            var contractName = _settings.ContractsRegistry.GetContractName(snapshot.GetType());
            var payload = _settings.Serializer.Serialize(snapshot, snapshot.GetType());
            _settings.Persistor.UpdateSnapshot(streamId, currentVersion, contractName, payload);
        }
    }
}
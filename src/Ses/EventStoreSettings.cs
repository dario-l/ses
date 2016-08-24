using System;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Conflicts;

namespace Ses
{
    internal class EventStoreSettings : IEventStoreSettings
    {
        public IEventStreamPersistor Persistor { get; internal set; }
        public IContractsRegistry ContractsRegistry { get; internal set; }
        public IUpConverterFactory UpConverterFactory { get; internal set; }
        public ISerializer Serializer { get; internal set; }
        public ILogger Logger { get; internal set; }
        public IConcurrencyConflictResolver ConcurrencyConflictResolver { get; internal set; }

        public void Validate()
        {
            if (Serializer == null) throw new InvalidOperationException("Can not build EventStore instance. Serializer is not initialized.");
            if (Persistor == null) throw new InvalidOperationException("Can not build EventStore instance. Persistor is not initialized.");
            if (ContractsRegistry == null) throw new InvalidOperationException("Can not build EventStore instance. ContractsRegistry is not initialized.");
        }
    }
}
using System;
using System.Reflection;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Abstracts.Logging;
using Ses.Conflicts;
using Ses.InMemory;

namespace Ses
{
    public class EventStoreBuilder
    {
        private readonly EventStoreSettings _settings;

        public EventStoreBuilder()
        {
            _settings = new EventStoreSettings { Logger = new NullLogger() };
        }

        public EventStoreBuilder WithDefaultContractsRegistry(params Assembly[] assemblies)
        {
            return WithContractsRegistry(new DefaultContractsRegistry(assemblies));
        }

        public EventStoreBuilder WithContractsRegistry(IContractsRegistry registry)
        {
            _settings.ContractsRegistry = registry;
            return this;
        }

        public EventStoreBuilder WithPersistor(IEventStreamPersistor persistor)
        {
            _settings.Persistor = persistor;
            return this;
        }

        public EventStoreBuilder WithInMemoryPersistor()
        {
            return WithPersistor(new InMemoryPersistor());
        }

        public EventStoreBuilder WithSerializer(ISerializer serializer)
        {
            _settings.Serializer = serializer;
            return this;
        }

        public EventStoreBuilder WithUpConverterFactory(IUpConverterFactory factory)
        {
            _settings.UpConverterFactory = factory;
            return this;
        }

        public EventStoreBuilder WithConcurrencyConflictResolver(IConcurrencyConflictResolver resolver)
        {
            _settings.ConcurrencyConflictResolver = resolver;
            return this;
        }

        public EventStoreBuilder WithDefaultConcurrencyConflictResolver(Action<IConcurrencyConflictResolverRegister> register)
        {
            var resolver = new DefaultConcurrencyConflictResolver();
            register(resolver);
            return WithConcurrencyConflictResolver(resolver);
        }

        public IEventStore Build()
        {
            _settings.Validate();
            return new EventStore(_settings);
        }
    }
}
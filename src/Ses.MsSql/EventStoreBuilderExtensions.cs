using System;

namespace Ses.MsSql
{
    public static class EventStoreBuilderExtensions
    {
        public static EventStoreBuilder WithMsSqlPersistor(this EventStoreBuilder builder, string connectionString, Action<IMsSqlPersistorBuilder> persistorBuilder = null)
        {
            var sqlPersistorBuilder = new MsSqlPersistorBuilder(builder.Logger, connectionString);
            var persistor = new MsSqlPersistor(sqlPersistorBuilder.Linearizer, connectionString);
            builder.WithPersistor(persistor);
            persistorBuilder?.Invoke(sqlPersistorBuilder);
            return builder;
        }
    }
}
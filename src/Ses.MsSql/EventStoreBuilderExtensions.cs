using System;

namespace Ses.MsSql
{
    public static class EventStoreBuilderExtensions
    {
        public static EventStoreBuilder WithMsSqlPersistor(this EventStoreBuilder builder, string connectionString)
        {
            builder.WithPersistor(new MsSqlPersistor(connectionString));
            return builder;
        }

        public static EventStoreBuilder WithMsSqlPersistor(this EventStoreBuilder builder, string connectionString, Action<IMsSqlPersistorBuilder> persistorBuilder)
        {
            builder.WithMsSqlPersistor(connectionString);
            persistorBuilder(new MsSqlPersistorBuilder(connectionString));
            return builder;
        }
    }
}
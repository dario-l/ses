using System;

namespace Ses.MsSql
{
    public static class EventStoreBuilderExtensions
    {
        public static EventStoreBuilder WithMsSqlPersistor(this EventStoreBuilder builder, string connectionString, Action<IMsSqlPersistorBuilder> persistorBuilder = null)
        {
            var sqlPersistorBuilder = new MsSqlPersistorBuilder(builder.Logger, connectionString);
            if (persistorBuilder != null) persistorBuilder(sqlPersistorBuilder);
            builder.WithPersistor(new MsSqlPersistor(sqlPersistorBuilder.Linearizer, connectionString));
            return builder;
        }
    }
}
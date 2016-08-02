namespace Ses.MsSql
{
    public static class EventStoreBuilderExtensions
    {
        public static EventStoreBuilder WithMsSqlPersistor(this EventStoreBuilder builder, string connectionString)
        {
            builder.WithPersistor(new MsSqlPersistor(connectionString));
            return builder;
        }
    }
}
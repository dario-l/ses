using Xunit;

namespace Ses.MsSql.Tests
{
    [CollectionDefinition("EventStore collection")]
    public class SimpleEventStoreCollection : ICollectionFixture<LocalDbFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
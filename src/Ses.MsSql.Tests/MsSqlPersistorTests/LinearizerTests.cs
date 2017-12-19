using System;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Extensions;
using Ses.Abstracts.Logging;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class LinearizerTests : TestsBase
    {
        [Fact]
        public async Task When_linearizing_then_not_throws()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent() });
            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            var exception = await Record.ExceptionAsync(
                async () =>
                {
                    using (var linearizer = new Linearizer(ConnectionString, new NullLogger(), TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(1)))
                    {
                        await linearizer.StartOnce().NotOnCapturedContext();
                    }
                }).NotOnCapturedContext();


            Assert.Null(exception);
        }

        public class FakeEvent : IEvent { }

        public LinearizerTests(LocalDbFixture fixture) : base(fixture) { }
    }
}
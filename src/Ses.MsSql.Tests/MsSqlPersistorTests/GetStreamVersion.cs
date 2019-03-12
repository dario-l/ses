using System;
using System.Threading.Tasks;
using Ses.Abstracts;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class GetStreamVersion : TestsBase
    {
        [Fact]
        public async Task When_getting_stream_version_reads_proper_value()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent(), new FakeEvent(), new FakeEvent() });

            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            var version = store.Advanced.GetStreamVersion(streamId);
            Assert.Equal(3, version);
        }

        [Fact]
        public async Task When_getting_stream_version_and_stream_doesnt_exists_returns_version_minus_1()
        {
            var store = await GetEventStore();
            var streamId = SequentialGuid.NewGuid();

            var version = store.Advanced.GetStreamVersion(streamId);
            Assert.Equal(-1, version);
        }

        public class FakeEvent : IEvent { }

        public GetStreamVersion(LocalDbFixture fixture) : base(fixture) { }
    }
}
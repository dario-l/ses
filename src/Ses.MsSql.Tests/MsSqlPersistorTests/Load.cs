using System;
using System.Linq;
using Ses.Abstracts;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class Load : TestsBase
    {
        [Fact]
        public async void When_loading_for_not_exited_stream_returns_null()
        {
            var store = await GetEventStore();

            var events = await store.LoadAsync(Guid.Empty, false);
            Assert.Null(events);
        }

        [Fact]
        public async void When_loading_stream_with_one_event_returns_committed_version_equals_1()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new[] { new FakeEvent() });
            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            var events = await store.LoadAsync(streamId, false);
            Assert.True(events.CommittedVersion == 1);
            Assert.True(events.CommittedEvents.Count() == 1);
        }

        public class FakeEvent : IEvent
        {
        }
    }
}
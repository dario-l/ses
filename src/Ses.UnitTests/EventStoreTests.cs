using System;
using System.Linq;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.UnitTests.Fakes;
using Xunit;

namespace Ses.UnitTests
{
    public class EventStoreTests
    {
        private readonly IEventStore _store;

        public EventStoreTests()
        {
            _store = new EventStoreBuilder()
                .WithInMemoryPersistor()
                .WithDefaultContractsRegistry(typeof(FakeEvent1).Assembly)
                .WithSerializer(new JsonNetSerializer())
                .Build();
        }

        [Fact]
        public async Task Load_event_stream_after_storing_2_messages_returns_2()
        {
            var streamId = Guid.Empty;
            var events = new IEvent[]
            {
                new FakeEvent1(),
                new FakeEvent2()
            };
            IEventStream stream = new EventStream(Guid.Empty, events);
            await _store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

            var restoredStream = await _store.Load(streamId, false);

            Assert.True(restoredStream.CommittedEvents.Count() == events.Length);
        }

        [Fact]
        public async Task Storing_empty_collection_of_events_do_nothing_and_after_load_returns_null()
        {
            var streamId = Guid.Empty;
            var events = new IEvent[0];
            IEventStream stream = new EventStream(Guid.Empty, events);
            await _store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

            var restoredStream = await _store.Load(streamId, false);

            Assert.Null(restoredStream);
        }
    }
}
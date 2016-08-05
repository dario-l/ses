using Ses.Abstracts;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class SaveChanges : TestsBase
    {
        [Fact]
        public async void Can_save_new_stream()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new[] { new FakeEvent() });
            await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);
        }

        [Fact]
        public async void Can_not_save_new_stream_when_expecting_nostream_and_the_same_streamid_exists()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new[] { new FakeEvent() });
            await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new[] { new FakeEvent() });
            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);
            });
        }

        [Fact]
        public async void Can_not_save_new_stream_when_expecting_any_and_the_same_streamid_exists()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new[] { new FakeEvent() });
            await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new[] { new FakeEvent() });
            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.SaveChanges(streamId, ExpectedVersion.Any, stream);
            });
        }

        [Fact]
        public async void Can_not_save_new_stream_when_expecting_1_but_stream_has_3()
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new[] { new FakeEvent(), new FakeEvent() });
            await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new[] { new FakeEvent() });
            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.SaveChanges(streamId, 1, stream);
            });
        }

        public class FakeEvent : IEvent
        {
        }
    }
}
using Ses.Abstracts;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class SaveChanges : TestsBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void Can_save_new_stream(bool isLockable)
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            var ex = await Record.ExceptionAsync(async () => {
                await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);
            });

            Assert.Null(ex);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void Can_not_save_new_stream_when_expecting_nostream_and_the_same_streamid_exists(bool isLockable)
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void Can_save_stream_with_any_events_even_when_stream_exists(bool isLockable)
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            var exception = await Record.ExceptionAsync(async () =>
            {
                await store.SaveChangesAsync(streamId, ExpectedVersion.Any, stream);
            });

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void Can_not_save_new_stream_when_expecting_1_but_stream_has_3(bool isLockable)
        {
            var store = await GetEventStore();

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent1(), new FakeEvent1() }, isLockable);
            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.SaveChangesAsync(streamId, 1, stream);
            });
        }

        public class FakeEvent1 : IEvent { }

        public SaveChanges(LocalDbFixture fixture) : base(fixture) { }
    }
}
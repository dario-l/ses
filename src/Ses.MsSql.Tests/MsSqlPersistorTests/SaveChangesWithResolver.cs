using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Conflicts;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class SaveChangesWithResolver : TestsBase
    {
        private readonly DefaultConcurrencyConflictResolver _resolver;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Can_not_save_new_stream_when_expecting_nostream_and_the_same_streamid_exists(bool isLockable)
        {
            var store = await GetEventStore(_resolver);

            var streamId = SequentialGuid.NewGuid();
            var stream = new EventStream(streamId, new IEvent[] { new FakeEvent1() }, isLockable);
            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

            stream = new EventStream(streamId, new IEvent[] { new FakeEvent2() }, isLockable);
            await store.SaveChangesAsync(streamId, 1, stream);
            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.SaveChangesAsync(streamId, 1, stream);
            });
        }

        public class FakeEvent1 : IEvent { }
        public class FakeEvent2 : IEvent { }
        public class FakeEvent3 : IEvent { }

        public SaveChangesWithResolver(LocalDbFixture fixture) : base(fixture)
        {
            _resolver = new DefaultConcurrencyConflictResolver();

            _resolver.RegisterConflicts(typeof(FakeEvent1), typeof(FakeEvent2));
        }
    }
}
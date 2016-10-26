using Xunit;

namespace Ses.Subscriptions.MsSql.Tests.MsSqlPollerStateRepositoryTests
{
    public class LoadingAndInserting : TestsBase
    {
        [Fact]
        public async void When_load_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () => await sut.LoadAsync("fakePoller"));

            Assert.Null(x);
        }

        [Fact]
        public async void When_load_with_empty_store_returns_empty_list()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            var result = await sut.LoadAsync("fakePoller");

            Assert.Empty(result);
        }

        [Fact]
        public async void When_inserting_or_updating_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () =>
            {
                await sut.InsertOrUpdateAsync(new PollerState("fakePoller", "fakeSource", "fakeHandler")); // inserting
                await sut.InsertOrUpdateAsync(new PollerState("fakePoller", "fakeSource", "fakeHandler")); // updating
            });

            Assert.Null(x);
        }

        [Fact]
        public async void When_inserting_and_updating_one_the_same_load_all_returns_one()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            await sut.InsertOrUpdateAsync(new PollerState("fakePoller", "fakeSource", "fakeHandler")); // inserting
            await sut.InsertOrUpdateAsync(new PollerState("fakePoller", "fakeSource", "fakeHandler")); // updating

            var result = await sut.LoadAsync("fakePoller");


            Assert.True(result.Length == 1);
        }

        [Fact]
        public async void When_inserting_two_different_load_for_first_returns_one()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            await sut.InsertOrUpdateAsync(new PollerState("fakePoller", "fakeSource", "fakeHandler"));
            await sut.InsertOrUpdateAsync(new PollerState("fakePoller2", "fakeSource2", "fakeHandler2"));

            var result = await sut.LoadAsync("fakePoller");


            Assert.True(result.Length == 1);
        }

        public LoadingAndInserting(LocalDbFixture fixture) : base(fixture)
        {
        }
    }
}

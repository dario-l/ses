using Xunit;

namespace Ses.Subscriptions.MsSql.Tests.MsSqlPoolerStateRepositoryTests
{
    public class LoadingAndInserting : TestsBase
    {
        [Fact]
        public async void When_load_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () => await sut.LoadAsync("fakePooler"));

            Assert.Null(x);
        }

        [Fact]
        public async void When_load_with_empty_store_returns_empty_list()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var result = await sut.LoadAsync("fakePooler");

            Assert.Empty(result);
        }

        [Fact]
        public async void When_inserting_or_updating_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () =>
            {
                await sut.InsertOrUpdateAsync(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // inserting
                await sut.InsertOrUpdateAsync(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // updating
            });

            Assert.Null(x);
        }

        [Fact]
        public async void When_inserting_and_updating_one_the_same_load_all_returns_one()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            await sut.InsertOrUpdateAsync(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // inserting
            await sut.InsertOrUpdateAsync(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // updating

            var result = await sut.LoadAsync("fakePooler");


            Assert.True(result.Length == 1);
        }

        [Fact]
        public async void When_inserting_two_different_load_for_first_returns_one()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            await sut.InsertOrUpdateAsync(new PoolerState("fakePooler", "fakeSource", "fakeHandler"));
            await sut.InsertOrUpdateAsync(new PoolerState("fakePooler2", "fakeSource2", "fakeHandler2"));

            var result = await sut.LoadAsync("fakePooler");


            Assert.True(result.Length == 1);
        }
    }
}

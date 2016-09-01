using Xunit;

namespace Ses.Subscriptions.MsSql.Tests.MsSqlPoolerStateRepositoryTests
{
    public class LoadingAndInserting : TestsBase
    {
        [Fact]
        public async void When_load_all_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () => await sut.LoadAll());

            Assert.Null(x);
        }

        [Fact]
        public async void When_load_all_with_empty_store_returns_empty_list()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var result = await sut.LoadAll();

            Assert.Empty(result);
        }

        [Fact]
        public async void When_inserting_or_updating_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () =>
            {
                await sut.InsertOrUpdate(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // inserting
                await sut.InsertOrUpdate(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // updating
            });

            Assert.Null(x);
        }

        [Fact]
        public async void When_inserting_and_updating_one_the_same_load_all_returns_one()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            await sut.InsertOrUpdate(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // inserting
            await sut.InsertOrUpdate(new PoolerState("fakePooler", "fakeSource", "fakeHandler")); // updating

            var result = await sut.LoadAll();


            Assert.True(result.Count == 1);
        }

        [Fact]
        public async void When_inserting_two_different_load_all_returns_two()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            await sut.InsertOrUpdate(new PoolerState("fakePooler", "fakeSource", "fakeHandler"));
            await sut.InsertOrUpdate(new PoolerState("fakePooler2", "fakeSource2", "fakeHandler2"));

            var result = await sut.LoadAll();


            Assert.True(result.Count == 2);
        }
    }
}

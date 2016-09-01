using Xunit;

namespace Ses.Subscriptions.MsSql.Tests.MsSqlPoolerStateRepositoryTests
{
    public class RemovingNotUsedStates : TestsBase
    {
        [Fact]
        public async void When_removing_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () => await sut.RemoveNotUsedStates("fakePooler", new string[0], new string[0]));

            Assert.Null(x);
        }

        [Fact]
        public async void When_removing_load_all_returns_empty_list()
        {
            await GetEventStore();

            var sut = new MsSqlPoolerStateRepository(ConnectionString).Initialize();

            var state = new PoolerState("fakePooler", "fakeSource", "fakeHandler");

            await sut.RemoveNotUsedStates(state.PoolerContractName, new[] { state.HandlerContractName }, new[] { state.SourceContractName });

            var result = await sut.LoadAll();

            Assert.Empty(result);
        }
    }
}

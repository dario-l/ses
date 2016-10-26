using Xunit;

namespace Ses.Subscriptions.MsSql.Tests.MsSqlPollerStateRepositoryTests
{
    public class RemovingNotUsedStates : TestsBase
    {
        [Fact]
        public async void When_removing_doesnt_throw()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            var x = await Record.ExceptionAsync(async () => await sut.RemoveNotUsedStatesAsync("fakePoller", new[] { "fakeHandler" }, new[] { "fakeSource" }));

            Assert.Null(x);
        }

        [Fact]
        public async void When_removing_load_all_returns_empty_list()
        {
            await GetEventStore();

            var sut = new MsSqlPollerStateRepository(ConnectionString).Initialize();

            var state = new PollerState("fakePoller", "fakeSource", "fakeHandler");

            await sut.RemoveNotUsedStatesAsync(state.PollerContractName, new[] { state.HandlerContractName }, new[] { state.SourceContractName });

            var result = await sut.LoadAsync("fakePoller");

            Assert.Empty(result);
        }

        public RemovingNotUsedStates(LocalDbFixture fixture) : base(fixture) { }
    }
}

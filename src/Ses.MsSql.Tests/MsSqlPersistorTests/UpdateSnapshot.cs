using System;
using Ses.Abstracts;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class UpdateSnapshot : TestsBase
    {
        [Fact]
        public async void When_updating_for_not_exited_stream_throws()
        {
            var store = await GetEventStore();

            await Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
            {
                await store.Advanced.UpdateSnapshotAsync(
                    Guid.Empty,
                    5,
                    new FakeSnapshot());
            });
        }

        public class FakeSnapshot : IMemento
        {
        }

        public UpdateSnapshot(LocalDbFixture fixture) : base(fixture)
        {
        }
    }
}
using System;
using System.Threading.Tasks;
using Ses.Abstracts.Extensions;
using Xunit;

namespace Ses.MsSql.Tests.MsSqlPersistorTests
{
    public class DeleteStream : TestsBase
    {
        [Fact]
        public async Task When_deleting_with_expected_version_any_doesnt_throw_even_if_not_exists()
        {
            var store = await GetEventStore();

            var x = await Record.ExceptionAsync(async () => await store.Advanced.DeleteStreamAsync(Guid.Empty, ExpectedVersion.Any)).NotOnCapturedContext();

            Assert.Null(x);
        }

        [Fact]
        public async Task When_deleting_with_expected_version_nostream_allways_throws()
        {
            var store = await GetEventStore();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await store.Advanced.DeleteStreamAsync(Guid.Empty, ExpectedVersion.NoStream);
            });
        }

        [Fact]
        public async Task When_deleting_with_expected_version_throw_if_not_exists()
        {
            var store = await GetEventStore();

            var x = await Record.ExceptionAsync(async () => await store.Advanced.DeleteStreamAsync(Guid.Empty, 1)).NotOnCapturedContext();

            Assert.NotNull(x);
        }

        public DeleteStream(LocalDbFixture fixture) : base(fixture) { }
    }
}

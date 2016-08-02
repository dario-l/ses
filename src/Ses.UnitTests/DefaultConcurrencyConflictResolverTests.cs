using Ses.Conflicts;
using Ses.UnitTests.Fakes;
using Xunit;

namespace Ses.UnitTests
{
    public class DefaultConcurrencyConflictResolverTests
    {
        [Fact]
        public void When_not_any_conflict_registered_then_returns_false()
        {
            var sut = new DefaultConcurrencyConflictResolver();
            var result = sut.ConflictsWith(typeof(FakeEvent1), new[]
            {
                typeof(FakeEvent2)
            });

            Assert.False(result);
        }

        [Fact]
        public void With_registered_conflict_returns_true()
        {
            var sut = new DefaultConcurrencyConflictResolver();
            sut.RegisterConflictList(typeof(FakeEvent1), typeof(FakeEvent2));

            var result = sut.ConflictsWith(typeof(FakeEvent1), new[]
            {
                typeof(FakeEvent2)
            });

            Assert.True(result);
        }
    }
}

using Ses.Abstracts.Converters;
using Ses.Abstracts.UnitTests.Fakes;
using Xunit;

namespace Ses.Abstracts.UnitTests
{
    public class DefaultEventConverterFactoryTests
    {
        [Fact]
        public void Creating_converter_instance_for_registered_event_converter_returns_converter_object()
        {
            var fakeContractType = typeof(FakeContract1);
            var sut = new DefaultUpConverterFactory(fakeContractType.Assembly);

            var converter = sut.CreateInstance(fakeContractType);
            Assert.NotNull(converter);
        }

        [Fact]
        public void Converting_from_FakeContract1_to_FakeContract2_succeded()
        {
            var fakeContractType = typeof(FakeContract1);
            IEvent event1 = new FakeContract1();
            var sut = new DefaultUpConverterFactory(fakeContractType.Assembly);

            var converter = sut.CreateInstance(fakeContractType);
            IEvent event2 = ((dynamic)converter).Convert((dynamic)event1);
            Assert.NotNull(event2 as FakeContract2);
        }

        [Fact]
        public void Creating_converter_instance_for_not_registered_event_converter_returns_null()
        {
            var fakeContractType = typeof(FakeContract1);
            var sut = new DefaultUpConverterFactory(fakeContractType.Assembly);

            var converter = sut.CreateInstance(typeof(FakeContract3));
            Assert.Null(converter);
        }
    }
}

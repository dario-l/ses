using System;
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
        public void Creating_converter_instance_for_not_registered_event_converter_returns_null()
        {
            var fakeContractType = typeof(FakeContract1);
            var sut = new DefaultUpConverterFactory(fakeContractType.Assembly);

            var converter = sut.CreateInstance(typeof(FakeContract3));
            Assert.Null(converter);
        }
    }
}

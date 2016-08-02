using System;
using System.Runtime.Serialization;
using Ses.Abstracts.Converters;
using Xunit;

namespace Ses.Abstracts.UnitTests
{
    public class DefaultEventConverterFactoryTests
    {
        [Fact]
        public void Creating_converter_instance_for_registered_event_converter_returns_converter_object()
        {
            var fakeContractType = typeof(FakeContract);
            var sut = new DefaultEventConverterFactory(fakeContractType.Assembly);

            var converter = sut.CreateInstance(fakeContractType);
            Assert.NotNull(converter);
        }

        [Fact]
        public void Creating_converter_instance_for_not_registered_event_converter_returns_null()
        {
            var fakeContractType = typeof(FakeContract);
            var sut = new DefaultEventConverterFactory(fakeContractType.Assembly);

            var converter = sut.CreateInstance(typeof(FakeContract2));
            Assert.Null(converter);
        }

        [Fact]
        public void Creating_converter_factory_for_duplicated_event_converter_throws()
        {
            var fakeContractType = typeof(FakeContract);
            Assert.Throws<InvalidOperationException>(() =>
            {
                new DefaultEventConverterFactory(fakeContractType.Assembly);
            });
        }

        [DataContract(Name = "FakeContract")]
        public class FakeContract : IEvent
        {

        }

        [DataContract(Name = "FakeContract2")]
        public class FakeContract2 : IEvent
        {

        }

        [DataContract(Name = "FakeContract3")]
        public class FakeContract3 : IEvent
        {

        }

        public class FakeUpConverter : IUpConvertEvent<FakeContract, FakeContract2>
        {
            public FakeContract2 Convert(FakeContract sourceEvent)
            {
                return new FakeContract2();
            }
        }

        public class FakeUpConverter32 : IUpConvertEvent<FakeContract3, FakeContract2>
        {
            public FakeContract2 Convert(FakeContract3 sourceEvent)
            {
                return new FakeContract2();
            }
        }

        public class FakeUpConverter31 : IUpConvertEvent<FakeContract3, FakeContract>
        {
            public FakeContract Convert(FakeContract3 sourceEvent)
            {
                return new FakeContract();
            }
        }
    }
}

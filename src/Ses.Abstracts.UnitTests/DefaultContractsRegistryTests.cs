using System;
using System.Runtime.Serialization;
using Ses.Abstracts.Contracts;
using Xunit;

namespace Ses.Abstracts.UnitTests
{
    public class DefaultContractsRegistryTests
    {
        [Fact]
        public void Getting_contract_name_for_not_registered_type_throws_NullReferenceException()
        {
            var sut = new DefaultContractsRegistry();

            Assert.Throws<NullReferenceException>(() =>
            {
                sut.GetContractName(typeof(string));
            });
        }

        [Fact]
        public void Getting_type_for_not_registered_contract_name_throws_NullReferenceException()
        {
            var sut = new DefaultContractsRegistry();

            Assert.Throws<NullReferenceException>(() =>
            {
                sut.GetType("not registered_contract name");
            });
        }

        [Fact]
        public void Getting_contract_name_for_registered_type_returns_name()
        {
            var fakeContractType = typeof(FakeContract);
            var sut = new DefaultContractsRegistry(fakeContractType.Assembly);
            
            var contractName = sut.GetContractName(fakeContractType);
            Assert.Equal(contractName, "FakeContract");
        }

        [Fact]
        public void Getting_type_for_registered_contract_returns_type()
        {
            var fakeContractType = typeof(FakeContract);
            var sut = new DefaultContractsRegistry(fakeContractType.Assembly);

            var contractType = sut.GetType("FakeContract");
            Assert.Equal(contractType, fakeContractType);
        }

        [DataContract(Name = "FakeContract")]
        internal class FakeContract : IEvent
        {
            
        }
    }
}

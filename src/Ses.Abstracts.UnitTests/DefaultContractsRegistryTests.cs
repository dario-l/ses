using System;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.UnitTests.Fakes;
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
        public void Getting_contract_name_with_datacontract_attribute_for_registered_type_returns_defined_name()
        {
            var fakeContractType = typeof(FakeContract1);
            var sut = new DefaultContractsRegistry(fakeContractType.Assembly);
            
            var contractName = sut.GetContractName(fakeContractType);
            Assert.Equal(contractName, "FakeContract1");
        }

        [Fact]
        public void Getting_contract_name_without_datacontract_attribute_for_registered_type_returns_type_fullname()
        {
            var fakeContractType = typeof(FakeContractWithoutDataContract);
            var sut = new DefaultContractsRegistry(fakeContractType.Assembly);

            var contractName = sut.GetContractName(fakeContractType);
            Assert.Equal(contractName, fakeContractType.FullName);
        }

        [Fact]
        public void Getting_type_for_registered_contract_returns_type()
        {
            var fakeContractType = typeof(FakeContract1);
            var sut = new DefaultContractsRegistry(fakeContractType.Assembly);

            var contractType = sut.GetType("FakeContract1");
            Assert.Equal(contractType, fakeContractType);
        }
    }
}

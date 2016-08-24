using System;

namespace Ses.Abstracts.Contracts
{
    public interface IContractsRegistry
    {
        Type GetType(string contractName);
        string GetContractName(Type type);
    }
}
using System;

namespace Ses.Abstracts.Contracts
{
    public interface IContractsRegistry
    {
        Type GetType(string contractName, bool ignore = false);
        string GetContractName(Type type);
    }
}
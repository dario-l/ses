using System;

namespace Ses.Abstracts
{
    public interface IContractsRegistry
    {
        Type GetType(string contractName);
    }
}
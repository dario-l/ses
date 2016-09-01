using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IPoolerStateRepository
    {
        Task InsertOrUpdate(PoolerState state);
        Task RemoveNotUsedStates(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames);
    }
}
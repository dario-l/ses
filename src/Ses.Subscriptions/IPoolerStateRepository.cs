using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IPoolerStateRepository
    {
        Task<IList<PoolerState>> LoadAll();
        Task InsertOrUpdate(PoolerState state);
        Task RemoveNotUsedStates(string poolerContractName, IEnumerable<string> handlerContractNames, IEnumerable<string> sourceContractNames);
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IPoolerStateRepository
    {
        Task<PoolerState[]> LoadAsync(string poolerContractName, CancellationToken cancellationToken = default(CancellationToken));
        Task InsertOrUpdateAsync(PoolerState state, CancellationToken cancellationToken = default(CancellationToken));
        Task RemoveNotUsedStatesAsync(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames, CancellationToken cancellationToken = default(CancellationToken));

        void RemoveNotUsedStates(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames);
    }
}
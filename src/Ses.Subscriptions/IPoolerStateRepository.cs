using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IPoolerStateRepository
    {
        Task<IReadOnlyCollection<PoolerState>> Load(string poolerContractName, CancellationToken cancellationToken = default(CancellationToken));
        Task InsertOrUpdate(PoolerState state, CancellationToken cancellationToken = default(CancellationToken));
        Task RemoveNotUsedStates(string poolerContractName, string[] handlerContractNames, string[] sourceContractNames, CancellationToken cancellationToken = default(CancellationToken));
    }
}
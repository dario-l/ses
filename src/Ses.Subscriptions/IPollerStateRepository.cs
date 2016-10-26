using System.Threading;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IPollerStateRepository
    {
        Task<PollerState[]> LoadAsync(string pollerContractName, CancellationToken cancellationToken = default(CancellationToken));
        Task InsertOrUpdateAsync(PollerState state, CancellationToken cancellationToken = default(CancellationToken));
        Task RemoveNotUsedStatesAsync(string pollerContractName, string[] handlerContractNames, string[] sourceContractNames, CancellationToken cancellationToken = default(CancellationToken));

        void RemoveNotUsedStates(string pollerContractName, string[] handlerContractNames, string[] sourceContractNames);
    }
}
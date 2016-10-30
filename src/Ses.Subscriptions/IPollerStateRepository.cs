using System.Threading;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IPollerStateRepository
    {
        Task InsertOrUpdateAsync(PollerState state, CancellationToken cancellationToken = default(CancellationToken));

        Task<PollerState[]> LoadAsync(string pollerContractName, CancellationToken cancellationToken = default(CancellationToken));
        Task CreateStatesAsync(PollerState[] states, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteStatesAsync(PollerState[] states, CancellationToken cancellationToken = default(CancellationToken));

        PollerState[] Load(string pollerContractName);
        void CreateStates(PollerState[] states);
        void DeleteStates(PollerState[] states);
    }
}
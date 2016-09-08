using System.Collections.Generic;
using System.Threading.Tasks;
using Ses.Abstracts.Contracts;

namespace Ses.Abstracts.Subscriptions
{
    public interface ISubscriptionEventSource
    {
        Task<IList<ExtractedEvent>> FetchAsync(IContractsRegistry registry, long lastVersion, int? subscriptionId);
        Task<int> CreateSubscriptionForContractsAsync(string name, params string[] contractNames);
        int CreateSubscriptionForContracts(string name, params string[] contractNames);
    }
}
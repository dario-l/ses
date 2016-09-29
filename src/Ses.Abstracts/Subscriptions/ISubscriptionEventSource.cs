using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;

namespace Ses.Abstracts.Subscriptions
{
    public interface ISubscriptionEventSource
    {
        Task<List<ExtractedEvent>> FetchAsync(IContractsRegistry registry, IUpConverterFactory upConverterFactory, long lastVersion, int? subscriptionId, CancellationToken cancellationToken = new CancellationToken());
        Task<int> CreateSubscriptionForContractsAsync(string name, string[] contractNames, CancellationToken cancellationToken = new CancellationToken());
        int CreateSubscriptionForContracts(string name, string[] contractNames);
    }
}
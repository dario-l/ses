using System.Diagnostics;
using System.Threading.Tasks;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;

namespace Ses.Samples.Subscriptions.ProcessManagers
{
    public class SomeProcessManager : IHandleAsync<ShoppingCartCreated>
    {
        public Task Handle(ShoppingCartCreated e, EventEnvelope envelope)
        {
            Debug.WriteLine("Act for " + e.GetType().Name);
            return Task.FromResult(0);
        }
    }
}
using System.Diagnostics;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;

namespace Ses.Samples.Subscriptions.ProcessManagers
{
    public class SomeProcessManager : IHandle<ShoppingCartCreated>
    {
        public void Handle(ShoppingCartCreated e, EventEnvelope envelope)
        {
            Debug.WriteLine("Act for " + e.GetType().Name);
        }
    }
}
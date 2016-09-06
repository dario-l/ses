using System.Diagnostics;
using System.Threading.Tasks;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;

namespace Ses.Samples.Subscriptions.EmailSenders
{
    public class EmailSender : IHandle<ShoppingCartCreated>
    {
        public Task Handle(ShoppingCartCreated e, EventEnvelope envelope)
        {
            Debug.WriteLine("Sending email when " + e.GetType().Name);
            return Task.FromResult(0);
        }
    }
}
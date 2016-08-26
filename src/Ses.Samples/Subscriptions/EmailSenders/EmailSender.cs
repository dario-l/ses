using System.Diagnostics;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;

namespace Ses.Samples.Subscriptions.EmailSenders
{
    public class EmailSender : IHandle<ShoppingCartCreated>
    {
        public void Handle(ShoppingCartCreated e, EventEnvelope envelope)
        {
            Debug.WriteLine("Sending email when " + e.GetType().Name);
        }
    }
}
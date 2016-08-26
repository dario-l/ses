using System.Diagnostics;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;

namespace Ses.Samples.Subscriptions.Projections
{
    public class CartProjection :
        IHandle<ShoppingCartCreated>,
        IHandle<ItemAddedToShoppingCart>,
        IHandle<ItemRemovedFromShoppingCart>
    {
        public void Handle(ShoppingCartCreated e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
        }

        public void Handle(ItemAddedToShoppingCart e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
        }

        public void Handle(ItemRemovedFromShoppingCart e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
        }
    }
}
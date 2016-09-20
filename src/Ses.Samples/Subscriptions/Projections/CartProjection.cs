using System.Diagnostics;
using System.Threading.Tasks;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;

namespace Ses.Samples.Subscriptions.Projections
{
    public class CartProjection :
        IHandleAsync<ShoppingCartCreated>,
        IHandleAsync<ItemAddedToShoppingCart>,
        IHandleAsync<ItemRemovedFromShoppingCart>,
        IHandleAsync<ItemRemovedFromShoppingCart2>
    {
        public Task Handle(ShoppingCartCreated e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
            return Task.FromResult(0);
        }

        public Task Handle(ItemAddedToShoppingCart e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
            return Task.FromResult(0);
        }

        public Task Handle(ItemRemovedFromShoppingCart e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
            return Task.FromResult(0);
        }

        public Task Handle(ItemRemovedFromShoppingCart2 e, EventEnvelope envelope)
        {
            Debug.WriteLine("Projected " + e.GetType().Name);
            return Task.FromResult(0);
        }
    }
}
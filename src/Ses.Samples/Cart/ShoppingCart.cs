using System;
using System.Linq;
using Ses.Domain;

namespace Ses.Samples.Cart
{
    public class ShoppingCart : Aggregate<ShoppingCartState>
    {
        public ShoppingCart() { }

        public ShoppingCart(Guid id, Guid customerId) //: this()
        {
            // Process new event, changing state and storing event as pending event
            // to be saved when aggregate is committed.
            Id = id;
            Apply(new ShoppingCartCreated(Id, customerId));
        }

        public void AddItem(Guid itemId, string name, int quantity)
        {
            Apply(new ItemAddedToShoppingCart(Id, itemId, name, quantity));
        }

        public void RemoveItem(Guid itemId)
        {
            if (State.Items.All(x => x.Id != itemId)) throw new InvalidOperationException($"Item {itemId} not found in cart {Id}.");
            Apply(new ItemRemovedFromShoppingCart(Id, itemId));
        }
    }
}
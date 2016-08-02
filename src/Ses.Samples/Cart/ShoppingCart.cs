using System;
using System.Collections.Generic;
using Ses.Domain;

namespace Ses.Samples.Cart
{
    public class ShoppingCart : Aggregate<ShoppingCartState>
    {
        public ShoppingCart()
        {
            State = new ShoppingCartState();
            Handles<ShoppingCartCreated>(State.OnCreated);
            Handles<ItemAddedToShoppingCart>(State.OnItemAddedToShoppingCart);
        }

        public ShoppingCart(Guid id, Guid customerId) : this()
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
    }
}
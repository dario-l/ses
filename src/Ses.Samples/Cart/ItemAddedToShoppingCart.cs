using System;
using System.Runtime.Serialization;
using Ses.Abstracts;

namespace Ses.Samples.Cart
{
    [DataContract(Name = "ItemAddedToShoppingCart")]
    public class ItemAddedToShoppingCart : IEvent
    {
        protected ItemAddedToShoppingCart() { }

        public Guid CartId { get; private set; }
        public Guid ItemId { get; private set; }
        public string Name { get; private set; }
        public int Quantity { get; private set; }

        public ItemAddedToShoppingCart(Guid cartId, Guid itemId, string name, int quantity)
        {
            CartId = cartId;
            ItemId = itemId;
            Name = name;
            Quantity = quantity;
        }
    }
}
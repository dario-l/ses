using System;
using System.Runtime.Serialization;
using Ses.Abstracts;

namespace Ses.Samples.Cart
{
    [DataContract(Name = "ItemRemovedFromShoppingCart")]
    public class ItemRemovedFromShoppingCart : IEvent
    {
        protected ItemRemovedFromShoppingCart() { }

        public Guid CartId { get; private set; }
        public Guid ItemId { get; private set; }

        public ItemRemovedFromShoppingCart(Guid cartId, Guid itemId)
        {
            CartId = cartId;
            ItemId = itemId;
        }
    }
}
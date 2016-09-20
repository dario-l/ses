using System;
using System.Runtime.Serialization;
using Ses.Abstracts;
using Ses.Abstracts.Converters;

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

    [DataContract(Name = "ItemRemovedFromShoppingCart2")]
    public class ItemRemovedFromShoppingCart2 : IEvent
    {
        protected ItemRemovedFromShoppingCart2() { }

        public Guid CartId { get; private set; }
        public Guid ItemId { get; private set; }

        public ItemRemovedFromShoppingCart2(Guid cartId, Guid itemId)
        {
            CartId = cartId;
            ItemId = itemId;
        }
    }

    public class ItemRemovedFromShoppingCartUpTo2 : IUpConvertEvent<ItemRemovedFromShoppingCart, ItemRemovedFromShoppingCart2>
    {
        public ItemRemovedFromShoppingCart2 Convert(ItemRemovedFromShoppingCart sourceEvent)
        {
            return new ItemRemovedFromShoppingCart2(sourceEvent.CartId, sourceEvent.ItemId);
        }
    }
}
using System.Collections.Generic;
using Ses.Abstracts;

namespace Ses.Samples.Cart
{
    public class ShoppingCartState : IMemento
    {
        public int Version { get; set; }
        public IList<CartItem> Items { get; set; }

        public void OnCreated(ShoppingCartCreated obj)
        {
            Items = new List<CartItem>();
        }

        public void OnItemAddedToShoppingCart(ItemAddedToShoppingCart obj)
        {
            Items.Add(new CartItem
            {
                Id = obj.ItemId,
                Name = obj.Name,
                Quantity = obj.Quantity
            });
        }
    }
}
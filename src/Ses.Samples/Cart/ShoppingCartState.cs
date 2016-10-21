using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ses.Abstracts;

namespace Ses.Samples.Cart
{
    [DataContract(Name = "ShoppingCartState")]
    public class ShoppingCartState : IMemento
    {
        public List<CartItem> Items { get; private set; }

        public ShoppingCartState()
        {
            Items = new List<CartItem>(2);
        }

        private void On(ItemAddedToShoppingCart obj)
        {
            Items.Add(new CartItem
            {
                Id = obj.ItemId,
                Name = obj.Name,
                Quantity = obj.Quantity
            });
        }

        private void On(ItemRemovedFromShoppingCart obj)
        {
            var item = Items.FirstOrDefault(x => x.Id == obj.ItemId);
            if (item != null) Items.Remove(item);
        }
    }
}
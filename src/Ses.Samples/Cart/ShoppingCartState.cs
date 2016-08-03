using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts;

namespace Ses.Samples.Cart
{
    public class ShoppingCartState : IMemento
    {
        public IList<CartItem> Items { get; private set; }

        private void On(ShoppingCartCreated obj)
        {
            Items = new List<CartItem>();
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
            var item = Items.SingleOrDefault(x => x.Id == obj.ItemId);
            if (item != null) Items.Remove(item);
        }
    }
}
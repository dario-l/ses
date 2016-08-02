using System;

namespace Ses.Samples.Cart
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
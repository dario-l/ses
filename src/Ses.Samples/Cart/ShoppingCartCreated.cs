using System;
using System.Runtime.Serialization;
using Ses.Abstracts;

namespace Ses.Samples
{
    [DataContract(Name = "ShoppingCartCreated")]
    public class ShoppingCartCreated : IEvent
    {
        public Guid CartId { get; private set; }
        public Guid CustomerId { get; private set; }

        public ShoppingCartCreated(Guid cartId, Guid customerId)
        {
            CartId = cartId;
            CustomerId = customerId;
        }
    }
}
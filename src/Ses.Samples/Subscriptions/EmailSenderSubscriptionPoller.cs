using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ses.Abstracts.Subscriptions;
using Ses.Samples.Cart;
using Ses.Subscriptions;

namespace Ses.Samples.Subscriptions
{
    [DataContract(Name = "EmailSenderSubscriptionPoller")]
    public class EmailSenderSubscriptionPoller : SubscriptionPoller
    {
        public EmailSenderSubscriptionPoller(ISubscriptionEventSource[] sources) : base(sources)
        {
            
        }

        protected override IEnumerable<Type> FindHandlerTypes()
        {
            return typeof(SampleRunner).Assembly.GetTypes().Where(x => x.Namespace != null && x.Namespace.EndsWith("EmailSenders"));
        }

        protected override IHandle CreateHandlerInstance(Type handlerType)
        {
            return Activator.CreateInstance(handlerType) as IHandle; // usually use IContainer
        }

        public override TimeSpan GetFetchTimeout()
        {
            return TimeSpan.FromSeconds(2); // 2 seconds for tests, usually no need to send emails immediately, set to higher value, eg. 10 minutes
        }

        protected override IEnumerable<Type> GetConcreteSubscriptionEventTypes()
        {
            yield return typeof(ShoppingCartCreated);
        }
    }
}
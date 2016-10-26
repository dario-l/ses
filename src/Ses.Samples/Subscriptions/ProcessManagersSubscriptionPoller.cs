using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ses.Abstracts.Subscriptions;
using Ses.Subscriptions;

namespace Ses.Samples.Subscriptions
{
    [DataContract(Name = "ProcessManagersSubscriptionPoller")]
    public class ProcessManagersSubscriptionPoller : SubscriptionPoller
    {
        public ProcessManagersSubscriptionPoller(ISubscriptionEventSource[] sources) : base(sources)
        {
            
        }

        protected override IEnumerable<Type> FindHandlerTypes()
        {
            return typeof(SampleRunner).Assembly.GetTypes().Where(x => x.Namespace != null && x.Namespace.EndsWith("ProcessManagers"));
        }

        protected override IHandle CreateHandlerInstance(Type handlerType)
        {
            return Activator.CreateInstance(handlerType) as IHandle; // usually use IContainer
        }
    }
}
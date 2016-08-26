using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ses.Abstracts.Subscriptions;
using Ses.Subscriptions;

namespace Ses.Samples.Subscriptions
{
    [DataContract(Name = "ProcessManagersSubscriptionPooler")]
    public class ProcessManagersSubscriptionPooler : SubscriptionPooler
    {
        public ProcessManagersSubscriptionPooler(ISubscriptionEventSource[] sources) : base(sources)
        {
            
        }

        protected override IEnumerable<Type> FindHandlerTypes()
        {
            return typeof(SampleRunner).Assembly.GetTypes();
        }

        protected override IHandle CreateHandlerInstance(Type handlerType)
        {
            return Activator.CreateInstance(handlerType) as IHandle; // usually use IContainer
        }
    }
}
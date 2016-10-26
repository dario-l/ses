using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Ses.Abstracts.Subscriptions;
using Ses.Subscriptions;

namespace Ses.Samples.Subscriptions
{
    [DataContract(Name = "ProjectionsSubscriptionPoller")]
    public class ProjectionsSubscriptionPoller : SubscriptionPoller
    {
        public ProjectionsSubscriptionPoller(ISubscriptionEventSource[] sources) : base(sources)
        {
            RetriesPolicy = PollerRetriesPolicy.Defaut();
        }

        protected override IEnumerable<Type> FindHandlerTypes()
        {
            return typeof(SampleRunner).Assembly.GetTypes().Where(x => x.Namespace != null && x.Namespace.EndsWith("Projections"));
        }

        protected override IHandle CreateHandlerInstance(Type handlerType)
        {
            return Activator.CreateInstance(handlerType) as IHandle; // usually use IContainer
        }

        public override TimeSpan? RunForDuration => TimeSpan.FromMinutes(5); // to run again use: subscriber.RunPooler(typeof(DenormalizersSubscriptionPooler));

        protected override void PreExecuting(int fetchedEventsCount)
        {
            Debug.WriteLine("PRE Fetched events: " + fetchedEventsCount);
        }

        protected override void PostExecuting(int fetchedEventsCount, Type[] dispatchedHandlers)
        {
            Debug.WriteLine("POST Fetched events: " + fetchedEventsCount + " Handled by: " + string.Join("|", dispatchedHandlers.Select(x => x.Name)));
        }
    }
}
using System;
using Ses.Abstracts;

namespace Ses.Domain
{
    public abstract class Aggregate<TSnapshot> : Aggregate, IAggregate<TSnapshot> where TSnapshot : class, IMemento, new()
    {
        protected Aggregate()
        {
            State = new TSnapshot();
        }

        protected TSnapshot State { get; set; }

        public virtual IAggregateSnapshot<TSnapshot> GetSnapshot()
        {
            return new AggregateSnapshot<TSnapshot>(CurrentVersion, State);
        }

        protected override void RestoreFromSnapshot(IMemento memento)
        {
            var snapshot = memento as TSnapshot;
            if (snapshot == null) throw new InvalidCastException("Memento is not type of " + typeof(TSnapshot).FullName);
            State = snapshot;
        }

        protected override void Invoke(IEvent @event)
        {
            RedirectToWhen.InvokeEventOptional(State, @event);
        }
    }
}

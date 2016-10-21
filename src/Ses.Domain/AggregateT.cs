using System;
using Ses.Abstracts;

namespace Ses.Domain
{
    public abstract class Aggregate<TState> : Aggregate where TState : class, IMemento, new()
    {
        protected Aggregate()
        {
            State = new TState();
        }

        protected TState State { get; set; }

        /// <summary>
        /// Returns snapshot from current state.
        /// </summary>
        /// <returns>Snapshot from current state</returns>
        public override IAggregateSnapshot GetSnapshot()
        {
            return new AggregateSnapshot(CurrentVersion, State);
        }

        protected override void RestoreFromSnapshot(IMemento memento)
        {
            var snapshot = memento as TState;
            if (snapshot == null) throw new InvalidCastException("Memento is not type of " + typeof(TState).FullName);
            State = snapshot;
        }

        protected internal override void Invoke(IEvent @event)
        {
            RedirectToWhen.InvokeEventOptional(State, @event);
        }
    }
}

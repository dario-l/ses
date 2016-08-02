using System;
using System.Collections.Generic;
using Ses.Abstracts;

namespace Ses.Domain
{
    /// <summary>
    /// Defines aggregate root
    /// </summary>
    public interface IAggregate
    {
        /// <summary>
        /// Returns aggregate identifier
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Returns aggregate committed version
        /// </summary>
        int CommittedVersion { get; }

        /// <summary>
        /// Returns new events registered during one scope of changes and clears internal collection.
        /// </summary>
        /// <returns>Collection of events</returns>
        IEnumerable<IEvent> TakeUncommittedEvents();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="history"></param>
        void Restore(Guid id, IEnumerable<IEvent> history);
    }

    /// <summary>
    /// Defines aggregate root with snapshoting
    /// </summary>
    public interface IAggregate<out TSnapshot> : IAggregate where TSnapshot : class, IMemento
    {
        /// <summary>
        /// Returns snapshot from current state.
        /// </summary>
        /// <returns>Snapshot from current state</returns>
        TSnapshot GetSnapshot();
    }
}
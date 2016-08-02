using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts;
using Ses.Domain.UnitTests.Fakes;
using Xunit;

namespace Ses.Domain.UnitTests
{
    public class AggregateTests
    {
        [Fact]
        public void Taking_uncommitted_events_from_empty_aggregate_returns_empty_list()
        {
            var sut = new FakeAggregate();
            var events = sut.TakeUncommittedEvents();
            Assert.NotNull(events);
        }

        [Fact]
        public void Taking_uncommitted_events_without_adding_any_from_restored_aggregate_returns_empty_list()
        {
            var sut = new FakeAggregate();
            IEnumerable<IEvent> events = new List<FakeEvent>
            {
                new FakeEvent()
            };
            sut.Restore(SequentialGuid.NewGuid(), events);
            events = sut.TakeUncommittedEvents();
            Assert.Equal(0, events.Count());
        }

        [Fact]
        public void After_taking_uncommitted_events_from_restored_aggregate_with_2_events_returns_committed_version_equals_2()
        {
            var sut = new FakeAggregate();
            IEnumerable<IEvent> events = new List<FakeEvent>
            {
                new FakeEvent(),
                new FakeEvent()
            };
            sut.Restore(SequentialGuid.NewGuid(), events);
            sut.TakeUncommittedEvents();
            Assert.Equal(2, sut.CommittedVersion);
        }

        [Fact]
        public void Restored_aggregate_with_snapshot_version_5_and_2_events_returns_committed_version_equals_7()
        {
            var sut = new FakeAggregate();
            IEnumerable<IEvent> events = new List<IEvent>
            {
                new FakeSnapshot { Version = 5 },
                new FakeEvent(),
                new FakeEvent()
            };
            sut.Restore(SequentialGuid.NewGuid(), events);
            Assert.Equal(7, sut.CommittedVersion);
        }

        [Fact]
        public void Restored_aggregate_with_snapshot_version_5_and_2_events_and_applied_one_uncommitted_event_returns_committed_version_equals_7()
        {
            var sut = new FakeAggregate();
            IEnumerable<IEvent> events = new List<IEvent>
            {
                new FakeSnapshot { Version = 5 },
                new FakeEvent(),
                new FakeEvent()
            };
            sut.Restore(SequentialGuid.NewGuid(), events);
            sut.BussinesOperation();
            Assert.Equal(7, sut.CommittedVersion);
        }

        [Fact]
        public void Taking_uncommitted_events_with_applied_one_uncommitted_event_returns_1()
        {
            var sut = new FakeAggregate();
            sut.BussinesOperation();
            Assert.Equal(1, sut.TakeUncommittedEvents().Count());
        }

        [Fact]
        public void Taking_snapshot_from_aggregate_with_applied_one_uncommitted_event_returns_that_event_was_applied()
        {
            var sut = new FakeAggregate();
            sut.BussinesOperation();
            var snapshot = sut.GetSnapshot();
            Assert.True(snapshot.FakeEventApplied);
        }
    }
}

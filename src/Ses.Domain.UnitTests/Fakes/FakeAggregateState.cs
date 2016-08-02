using Ses.Abstracts;

namespace Ses.Domain.UnitTests.Fakes
{
    public class FakeAggregateState : IMemento
    {
        public int Version { get; set; }
        public bool FakeEventApplied { get; private set; }

        public void OnFakeEvent(FakeEvent obj)
        {
            FakeEventApplied = true;
        }
    }
}
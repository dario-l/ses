using Ses.Abstracts;

namespace Ses.Domain.UnitTests.Fakes
{
    public class FakeAggregateState : IMemento
    {
        public bool FakeEventApplied { get; private set; }

        private void On(FakeEvent obj)
        {
            FakeEventApplied = true;
        }
    }
}
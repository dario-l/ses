namespace Ses.Domain.UnitTests.Fakes
{
    public class FakeAggregate : Aggregate<FakeAggregateState>
    {
        public FakeAggregate()
        {
            Handles<FakeEvent>(State.OnFakeEvent);
        }

        public void BussinesOperation()
        {
            Apply(new FakeEvent());
        }
    }
}
namespace Ses.Domain.UnitTests.Fakes
{
    public class FakeAggregate : Aggregate<FakeAggregateState>
    {
        public void BussinesOperation()
        {
            Apply(new FakeEvent());
        }
    }
}
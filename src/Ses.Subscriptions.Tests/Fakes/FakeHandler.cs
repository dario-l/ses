using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions.Tests.Fakes
{
    public class FakeHandler : IHandle<FakeEvent>
    {
        public void Handle(FakeEvent e, EventEnvelope envelope)
        {
            
        }
    }
}
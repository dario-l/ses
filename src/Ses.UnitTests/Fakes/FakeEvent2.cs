using System.Runtime.Serialization;
using Ses.Abstracts;

namespace Ses.UnitTests.Fakes
{
    [DataContract(Name = "FakeEvent2")]
    public class FakeEvent2 : IEvent
    {
    }
}
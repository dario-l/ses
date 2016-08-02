using System.Runtime.Serialization;
using Ses.Abstracts;

namespace Ses.UnitTests.Fakes
{
    [DataContract(Name = "FakeEvent1")]
    public class FakeEvent1 : IEvent
    {
    }
}
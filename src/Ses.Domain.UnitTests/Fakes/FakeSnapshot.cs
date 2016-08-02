using Ses.Abstracts;

namespace Ses.Domain.UnitTests.Fakes
{
    public class FakeSnapshot : IMemento
    {
        public int Version { get; set; }
    }
}
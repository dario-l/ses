using Ses.Abstracts.Converters;

namespace Ses.Abstracts.UnitTests.Fakes
{
    public class FakeUpConverter12 : IUpConvertEvent<FakeContract1, FakeContract2>
    {
        public FakeContract2 Convert(FakeContract1 sourceEvent)
        {
            return new FakeContract2();
        }
    }
}
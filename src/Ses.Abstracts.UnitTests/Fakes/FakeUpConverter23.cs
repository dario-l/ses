using Ses.Abstracts.Converters;

namespace Ses.Abstracts.UnitTests.Fakes
{
    public class FakeUpConverter23 : IUpConvertEvent<FakeContract2, FakeContract3>
    {
        public FakeContract3 Convert(FakeContract2 sourceEvent)
        {
            return new FakeContract3();
        }
    }
}
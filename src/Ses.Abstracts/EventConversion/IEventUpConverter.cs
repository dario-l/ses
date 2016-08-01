namespace Ses.Abstracts.EventConversion
{
    public interface IEventUpConverter
    {
        IEvent Convert(IEvent oldEvent);
    }
}
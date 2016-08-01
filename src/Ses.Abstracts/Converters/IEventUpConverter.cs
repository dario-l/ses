namespace Ses.Abstracts.Converters
{
    public interface IEventUpConverter
    {
        IEvent Convert(IEvent oldEvent);
    }
}
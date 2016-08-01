namespace Ses.Abstracts.Converters
{
    public class EventUpConverter : IEventUpConverter
    {
        private readonly IEventConverterFactory _converterFactory;

        public EventUpConverter(IEventConverterFactory converterFactory)
        {
            _converterFactory = converterFactory;
        }

        public IEvent Convert(IEvent @event)
        {
            var converter = (dynamic)_converterFactory.CreateInstance(@event.GetType());
            if (converter == null)
            {
                return @event;
            }
            var convertedEvent = converter.Convert(@event) as IEvent;
            return ReferenceEquals(convertedEvent, @event) ? @event : convertedEvent;
        }
    }
}
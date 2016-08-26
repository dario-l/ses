using System;

namespace Ses.Abstracts.Subscriptions
{
    public class ExtractedEvent
    {
        public ExtractedEvent(EventEnvelope envelope, Type sourceType)
        {
            Envelope = envelope;
            SourceType = sourceType;
        }

        public EventEnvelope Envelope { get; private set; }
        public Type SourceType { get; private set; }
    }
}
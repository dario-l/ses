using System;

namespace Ses.Abstracts.EventConversion
{
    public interface IEventConverterFactory
    {
        IUpconvertEvent CreateInstance(Type type);
    }
}
using System;

namespace Ses.Abstracts.Converters
{
    public interface IEventConverterFactory
    {
        IUpconvertEvent CreateInstance(Type type);
    }
}
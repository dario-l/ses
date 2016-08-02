using System;

namespace Ses.Abstracts.Converters
{
    public interface IEventConverterFactory
    {
        IUpConvertEvent CreateInstance(Type type);
    }
}
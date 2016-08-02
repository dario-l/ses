using System;

namespace Ses.Abstracts.Converters
{
    public interface IUpConverterFactory
    {
        IUpConvertEvent CreateInstance(Type type);
    }
}
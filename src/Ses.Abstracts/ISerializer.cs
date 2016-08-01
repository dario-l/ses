using System;

namespace Ses.Abstracts
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T data, Type dataType = null);
        T Deserialize<T>(byte[] data, Type dataType);
    }
}
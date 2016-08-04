using System;
using Ses.Abstracts;

namespace Ses.Samples.Serializers
{
    public class NullSerializer : ISerializer
    {
        public byte[] Serialize<T>(T data, Type dataType = null)
        {
            return new byte[0];
        }

        public T Deserialize<T>(byte[] data, Type dataType)
        {
            return default(T);
        }
    }
}
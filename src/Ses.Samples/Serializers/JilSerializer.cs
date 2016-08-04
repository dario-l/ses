using System;
using System.Text;
using Ses.Abstracts;

namespace Ses.Samples.Serializers
{
    public class JilSerializer : ISerializer
    {
        public byte[] Serialize<T>(T data, Type dataType = null)
        {
            return Encoding.UTF8.GetBytes(Jil.JSON.SerializeDynamic(data));
        }

        public T Deserialize<T>(byte[] data, Type dataType)
        {
            return (T)Jil.JSON.Deserialize(Encoding.UTF8.GetString(data), dataType);
        }
    }
}

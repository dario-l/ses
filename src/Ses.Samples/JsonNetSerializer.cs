using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ses.Abstracts;

namespace Ses.Samples
{
    public class JsonNetSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonNetSerializer()
        {
            _settings = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new IncludeNonPublicMembersContractResolver(),
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public byte[] Serialize<T>(T data, Type dataType = null)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, _settings));
        }

        public T Deserialize<T>(byte[] data, Type dataType)
        {
            return (T)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), dataType, _settings);
        }

        private class IncludeNonPublicMembersContractResolver : DefaultContractResolver
        {
            public IncludeNonPublicMembersContractResolver()
            {
#pragma warning disable 618
                DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
#pragma warning restore 618
            }

            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                var members = base.GetSerializableMembers(objectType);
                return members.Where(m => !m.Name.EndsWith("k__BackingField") && m.MemberType == MemberTypes.Property).ToList();
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                property.Ignored = false;
                return property;
            }
        }
    }
}
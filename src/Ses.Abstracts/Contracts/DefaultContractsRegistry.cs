using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Ses.Abstracts.Contracts
{
    public class DefaultContractsRegistry : IContractsRegistry
    {
        readonly IDictionary<string, Type> _contract2Type = new Dictionary<string, Type>();
        readonly IDictionary<Type, string> _type2Contract = new Dictionary<Type, string>();

        public DefaultContractsRegistry(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0) assemblies = new[]
            {
                Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()
            };

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(IsKnownType))
                {
                    var name = CreateContractName(type);
                    if (_contract2Type.ContainsKey(name))
                        throw new InvalidOperationException($"Can not register type {type.FullName} because contract name '{name}' is already registered.");

                    _contract2Type.Add(name, type);
                    _type2Contract.Add(type, name);
                }
            }
        }

        private static string CreateContractName(Type type)
        {
            var attr = type.GetCustomAttribute<DataContractAttribute>(false);
            var name = (attr == null ? type.FullName : attr.Namespace + "." + attr.Name).Trim('.');
            if (name.Length > 225)
                throw new InvalidDataContractException($"The length of contract name for type {type.FullName} must be shorten than 225. Current '{name}' is {name.Length}.");

            return name;
        }

        private static bool IsKnownType(Type t)
        {
            return t.IsClass
                   && (typeof(IEvent).IsAssignableFrom(t)
                       || typeof(IMemento).IsAssignableFrom(t)
                       //|| typeof(IHandle).IsAssignableFrom(t)
                       //|| typeof(IStreamReader).IsAssignableFrom(t)
                       );
        }

        public string GetContractName(Type type)
        {
            string result;
            if (!_type2Contract.TryGetValue(type, out result)) throw new NullReferenceException($"Contract name for type '{type.FullName}' not found!");
            return result;
        }

        public Type GetType(string contractName)
        {
            Type result;
            if (!_contract2Type.TryGetValue(contractName, out result)) throw new NullReferenceException($"Type for contract name '{contractName}' not found!");
            return result;
        }
    }
}

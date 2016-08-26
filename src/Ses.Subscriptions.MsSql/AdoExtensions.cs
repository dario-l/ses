using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Ses.Subscriptions.MsSql
{
    internal static class AdoExtensions
    {
        public static IDbCommand AddInputParam(this IDbCommand cmd, string name, DbType type, object value)
        {
            var p = cmd.CreateParameter();
            p.DbType = type;
            p.ParameterName = name;
            p.Value = value;
            p.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(p);
            return cmd;
        }

        public static IDbCommand AddArrayParameters<T>(this IDbCommand cmd, string name, DbType type, IEnumerable<T> values)
        {
            name = name.StartsWith("@") ? name : "@" + name;
            var names = string.Join(", ", values.Select((value, i) =>
            {
                var paramName = name + i;
                cmd.AddInputParam(paramName, type, value);
                return paramName;
            }));
            cmd.CommandText = cmd.CommandText.Replace(name, names);
            return cmd;
        }
    }
}
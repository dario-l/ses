using System.Data;
using System.Linq;

namespace Ses.Subscriptions.MsSql
{
    internal static class AdoExtensions
    {
        public static IDbCommand AddInputParam<T>(this IDbCommand cmd, string name, DbType type, T value)
        {
            var param = cmd.CreateParameter();
            param.DbType = type;
            param.ParameterName = name;
            param.Value = value;
            param.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(param);
            return cmd;
        }

        public static IDbCommand AddArrayParameters<T>(this IDbCommand cmd, string name, DbType type, T[] values)
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
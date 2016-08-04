using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.MsSql
{
    internal static class AdoExtensions
    {
        public static SqlCommand AddInputParam(this SqlCommand cmd, string name, DbType type, object value)
        {
            var p = cmd.CreateParameter();
            p.DbType = type;
            p.ParameterName = name;
            p.Value = value;
            p.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(p);
            return cmd;
        }

        public static SqlCommand AddInputParam(this SqlCommand cmd, SqlParameter parameter, object value)
        {
            parameter.Value = value;
            cmd.Parameters.Add(parameter);
            return cmd;
        }

        public static SqlCommand OpenAndCreateCommand(this SqlConnection cnn, string commandText)
        {
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = commandText;
            return cmd;
        }

        public static async Task<SqlCommand> OpenAndCreateCommandAsync(this SqlConnection cnn, string commandText, CancellationToken cancelationToken)
        {
            await cnn.OpenAsync(cancelationToken).ConfigureAwait(false);
            var cmd = cnn.CreateCommand();
            cmd.CommandText = commandText;
            return cmd;
        }
    }
}

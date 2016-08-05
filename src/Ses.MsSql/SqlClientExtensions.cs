using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.MsSql
{
    internal static class SqlClientExtensions
    {
        private const short duplicateKeyViolationErrorNumber = 2627;
        private const string wrongExpectedVersionKey = "WrongExpectedVersion";

        public static bool IsUniqueConstraintViolation(this SqlException e, string indexName = null)
        {
            return e.Number == duplicateKeyViolationErrorNumber && (indexName == null || e.Message.Contains($"'{indexName}'"));
        }

        public static bool IsWrongExpectedVersionRised(this SqlException e)
        {
            return e.Message.StartsWith(wrongExpectedVersionKey, StringComparison.OrdinalIgnoreCase);
        }

        public static SqlCommand AddInputParam(this SqlCommand cmd, string name, DbType type, object value, bool isNullable = false)
        {
            var p = cmd.CreateParameter();
            p.DbType = type;
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            p.IsNullable = isNullable;
            p.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(p);
            return cmd;
        }

        public static SqlCommand AddInputParam(this SqlCommand cmd, SqlParameter parameter, object value)
        {
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = value;
            parameter.IsNullable = true;
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Extensions;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions.MsSql
{
    [DataContract(Name = "Ses.Subscriptions.MsSql.MsSqlEventSource")]
    public class MsSqlEventSource : ISubscriptionEventSource
    {
        private readonly ISerializer _serializer;
        private readonly string _connectionString;
        private const string selectEventsProcedure = "SesSelectTimelineEvents";
        private const string selectSubscriptionEventsProcedure = "SesSelectTimelineSubscriptionEvents";
        private const string sequenceIdParamName = "@SequenceId";
        private const string subscriptionIdParamName = "@SubscriptionId";
        private static readonly Type metadataType = typeof(Dictionary<string, string>);

        public MsSqlEventSource(ISerializer serializer, string connectionString)
        {
            _serializer = serializer;
            _connectionString = connectionString;
        }

        protected virtual void OnSqlCommandCreated(SqlCommand cmd, long lastVersion, int? subscriptionId)
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = subscriptionId.HasValue ? selectSubscriptionEventsProcedure : selectEventsProcedure;
            cmd.AddInputParam(sequenceIdParamName, DbType.Int64, lastVersion);
            if (subscriptionId.HasValue)
            {
                cmd.AddInputParam(subscriptionIdParamName, DbType.Int32, subscriptionId.Value);
            }
        }

        public async Task<IList<ExtractedEvent>> FetchAsync(IContractsRegistry registry, long lastVersion, int? subscriptionId)
        {
            var extractedEvents = new List<ExtractedEvent>(100);
            using (var cnn = new SqlConnection(_connectionString))
            {
                await cnn.OpenAsync().NotOnCapturedContext();
                var cmd = cnn.CreateCommand();
                OnSqlCommandCreated(cmd, lastVersion, subscriptionId);
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult).NotOnCapturedContext())
                {
                    while (await reader.ReadAsync().NotOnCapturedContext())
                    {
                        var eventType = registry.GetType(reader.GetString(3));
                        var @event = _serializer.Deserialize<IEvent>((byte[])reader[4], eventType);
                        var metadata = reader[7] == DBNull.Value
                            ? null
                            : _serializer.Deserialize<IDictionary<string, object>>((byte[])reader[7], metadataType);

                        var envelope = new EventEnvelope(
                            reader.GetGuid(0), // StreamId 0
                            reader.GetGuid(2), // CommitId 2
                            reader.GetDateTime(5), // CreatedAtUtc 5
                            reader.GetInt64(6), // EventId 6
                            reader.GetInt32(1), // Version 1
                            @event, // 6
                            metadata); // 7

                        //Fetched(envelope, GetType());

                        extractedEvents.Add(new ExtractedEvent(envelope, GetType()));
                    }
                }
            }
            return extractedEvents;
        }

        public virtual async Task<int> CreateSubscriptionForContractsAsync(string name, params string[] contractNames)
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                try
                {
                    await cnn.OpenAsync().NotOnCapturedContext();
                    cmd.Transaction = cnn.BeginTransaction();
                    cmd.CommandText = "IF(SELECT Count(1) FROM StreamsSubscriptions WHERE Name = @Name) = 0 BEGIN INSERT INTO StreamsSubscriptions(Name) OUTPUT Inserted.ID VALUES(@Name); END;";
                    cmd.AddInputParam("@Name", DbType.String, name);
                    var subscriptionId = (int)(await cmd.ExecuteScalarAsync().NotOnCapturedContext());
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO StreamsSubscriptionContracts(StreamsSubscriptionId,EventContractName)VALUES(@SubscriptionId,@ContractName)";
                    cmd.AddInputParam("@SubscriptionId", DbType.Int32, subscriptionId);
                    cmd.AddInputParam("@ContractName", DbType.String, null);
                    foreach (var contractName in contractNames)
                    {
                        cmd.Parameters[1].Value = contractName;
                        await cmd.ExecuteNonQueryAsync().NotOnCapturedContext();
                    }
                    cmd.Transaction.Commit();
                    return subscriptionId;
                }
                catch
                {
                    cmd.Transaction?.Rollback();
                    throw;
                }
                finally
                {
                    cnn.Close();
                }
            }
        }

        public int CreateSubscriptionForContracts(string name, params string[] contractNames)
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                try
                {
                    cnn.Open();
                    cmd.Transaction = cnn.BeginTransaction();
                    cmd.CommandText = "IF(SELECT Count(1) FROM StreamsSubscriptions WHERE Name = @Name) = 0 BEGIN INSERT INTO StreamsSubscriptions(Name) OUTPUT Inserted.ID VALUES(@Name); END;";
                    cmd.AddInputParam("@Name", DbType.String, name);
                    var subscriptionId = (int)cmd.ExecuteScalar();
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO StreamsSubscriptionContracts(StreamsSubscriptionId,EventContractName)VALUES(@SubscriptionId,@ContractName)";
                    cmd.AddInputParam("@SubscriptionId", DbType.Int32, subscriptionId);
                    cmd.AddInputParam("@ContractName", DbType.String, null);
                    foreach (var contractName in contractNames)
                    {
                        cmd.Parameters[1].Value = contractName;
                        cmd.ExecuteNonQuery();
                    }
                    cmd.Transaction.Commit();
                    return subscriptionId;
                }
                catch
                {
                    cmd.Transaction?.Rollback();
                    throw;
                }
                finally
                {
                    cnn.Close();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions.MsSql
{
    [DataContract(Name = "Ses.Subscriptions.MsSql.MsSqlEventSource")]
    public class MsSqlEventSource : ISubscriptionEventSource
    {
        private readonly ISerializer _serializer;
        private readonly string _connectionString;
        private const string selectTimelineEventsProcedure = "ESSelectTimelineEvents";
        private const string sequenceIdParamName = "@SequenceId";
        private static readonly Type metadataType = typeof(Dictionary<string, string>);

        public MsSqlEventSource(ISerializer serializer, string connectionString)
        {
            _serializer = serializer;
            _connectionString = connectionString;
        }

        protected virtual void OnSqlCommandCreated(SqlCommand cmd, long lastVersion)
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = selectTimelineEventsProcedure;
            cmd.AddInputParam(sequenceIdParamName, DbType.Int64, lastVersion);
        }

        public async Task<IList<ExtractedEvent>> Fetch(IContractsRegistry registry, long lastVersion)
        {
            var extractedEvents = new List<ExtractedEvent>(100);
            using (var cnn = new SqlConnection(_connectionString))
            {
                await cnn.OpenAsync().NotOnCapturedContext();
                var cmd = cnn.CreateCommand();
                OnSqlCommandCreated(cmd, lastVersion);
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult).NotOnCapturedContext())
                {
                    while (await reader.ReadAsync().NotOnCapturedContext())
                    {
                        var eventType = registry.GetType(reader.GetString(3));
                        var @event = _serializer.Deserialize<IEvent>((byte[])reader[4], eventType);
                        var metadata = reader[7] == DBNull.Value
                            ? null
                            : _serializer.Deserialize<IDictionary<string, string>>((byte[])reader[7], metadataType);

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
    }
}
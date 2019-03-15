using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Abstracts.Extensions;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions.MsSql
{
    [DataContract(Name = "Ses.Subscriptions.MsSql.MsSqlEventSource")]
    public class MsSqlEventSource : ISubscriptionEventSource
    {
        private static readonly Type metadataType = typeof(Dictionary<string, object>);

        private readonly ISerializer _serializer;
        private readonly string _connectionString;
        private readonly int _fetchLimit;

        private const string selectEventsProcedure = "SesSelectTimelineEvents";
        private const string selectSubscriptionEventsProcedure = "SesSelectTimelineSubscriptionEvents";
        private const string limitParamName = "@Limit";
        private const string sequenceIdParamName = "@SequenceId";
        private const string subscriptionIdParamName = "@SubscriptionId";
        
        private const byte colIndexForContractName = 0;
        private const byte colIndexForEventPayload = 1;
        private const byte colIndexForMetaPayload = 2;
        private const byte colIndexForStreamId = 3;
        private const byte colIndexForCommitId = 4;
        private const byte colIndexForCreatedAtUtc = 5;
        private const byte colIndexForEventId = 6;
        private const byte colIndexForVersion = 7;

        public MsSqlEventSource(ISerializer serializer, string connectionString, int fetchLimit = 100)
        {
            _serializer = serializer;
            _connectionString = connectionString;
            _fetchLimit = fetchLimit;
        }

        protected virtual void OnSqlCommandCreated(SqlCommand cmd, long lastVersion, int? subscriptionId, int fetchLimit)
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = subscriptionId.HasValue ? selectSubscriptionEventsProcedure : selectEventsProcedure;
            cmd.AddInputParam(limitParamName, DbType.Int32, fetchLimit);
            cmd.AddInputParam(sequenceIdParamName, DbType.Int64, lastVersion);
            if (subscriptionId.HasValue)
            {
                cmd.AddInputParam(subscriptionIdParamName, DbType.Int32, subscriptionId.Value);
            }
        }

        public async Task<List<ExtractedEvent>> FetchAsync(IContractsRegistry registry, IUpConverterFactory upConverterFactory, long lastVersion, int? subscriptionId, CancellationToken cancellationToken = new CancellationToken())
        {
            List<ExtractedEvent> extractedEvents = null;
            using (var cnn = new SqlConnection(_connectionString))
            {
                await cnn.OpenAsync(cancellationToken).NotOnCapturedContext();
                var cmd = cnn.CreateCommand();
                OnSqlCommandCreated(cmd, lastVersion, subscriptionId, _fetchLimit);
                using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken).NotOnCapturedContext())
                {
                    if (reader.HasRows)
                    {
                        extractedEvents = new List<ExtractedEvent>(_fetchLimit);
                        while (await reader.ReadAsync(cancellationToken).NotOnCapturedContext())
                        {
                            var eventType = registry.GetType(await reader.GetFieldValueAsync<string>(colIndexForContractName, cancellationToken).NotOnCapturedContext(), true);
                            if (eventType == null) continue;

                            var @event = UpConvert(upConverterFactory, eventType, _serializer.Deserialize<IEvent>(await reader.GetFieldValueAsync<byte[]>(colIndexForEventPayload, cancellationToken).NotOnCapturedContext(), eventType));
                            var metadata = await reader.IsDBNullAsync(colIndexForMetaPayload, cancellationToken).NotOnCapturedContext()
                                ? null
                                : _serializer.Deserialize<IDictionary<string, object>>(await reader.GetFieldValueAsync<byte[]>(colIndexForMetaPayload, cancellationToken).NotOnCapturedContext(), metadataType);

                            var envelope = new EventEnvelope(
                                await reader.GetFieldValueAsync<Guid>(colIndexForStreamId, cancellationToken).NotOnCapturedContext(), // StreamId 0
                                await reader.GetFieldValueAsync<Guid>(colIndexForCommitId, cancellationToken).NotOnCapturedContext(), // CommitId 1
                                await reader.GetFieldValueAsync<DateTime>(colIndexForCreatedAtUtc, cancellationToken).NotOnCapturedContext(), // CreatedAtUtc 2
                                await reader.GetFieldValueAsync<long>(colIndexForEventId, cancellationToken).NotOnCapturedContext(), // EventId 3
                                await reader.GetFieldValueAsync<int>(colIndexForVersion, cancellationToken).NotOnCapturedContext(), // Version 4
                                @event,
                                metadata);

                            extractedEvents.Add(new ExtractedEvent(envelope, GetType()));
                        }
                    }
                }
            }
            return extractedEvents ?? new List<ExtractedEvent>(0);
        }

        private static IEvent UpConvert(IUpConverterFactory upConverterFactory, Type eventType, IEvent @event)
        {
            if (upConverterFactory == null) return @event;

            var upConverter = upConverterFactory.CreateInstance(eventType);
            while (upConverter != null)
            {
                @event = ((dynamic)upConverter).Convert((dynamic)@event);
                upConverter = upConverterFactory.CreateInstance(@event.GetType());
            }
            return @event;
        }

        public virtual async Task<int> CreateSubscriptionForContractsAsync(string name, string[] contractNames, CancellationToken cancellationToken = new CancellationToken())
        {
            using (var cnn = new SqlConnection(_connectionString))
            using (var cmd = cnn.CreateCommand())
            {
                try
                {
                    await cnn.OpenAsync(cancellationToken).NotOnCapturedContext();
                    cmd.Transaction = cnn.BeginTransaction();
                    cmd.CommandText = @"SELECT TOP 1 Id FROM StreamsSubscriptions WHERE Name = @Name";
                    cmd.AddInputParam("@Name", DbType.String, name);
                    var subscriptionId = (int?)await cmd.ExecuteScalarAsync(cancellationToken).NotOnCapturedContext();
                    if (subscriptionId == null)
                    {
                        cmd.CommandText = @"INSERT INTO StreamsSubscriptions(Name) OUTPUT Inserted.ID VALUES(@Name);";
                        subscriptionId = (int)await cmd.ExecuteScalarAsync(cancellationToken).NotOnCapturedContext();
                        cmd.Parameters.Clear();
                        cmd.CommandText = @"INSERT INTO StreamsSubscriptionContracts(StreamsSubscriptionId,EventContractName)VALUES(@SubscriptionId,@ContractName)";
                        cmd.AddInputParam("@SubscriptionId", DbType.Int32, subscriptionId);
                        cmd.AddInputParam("@ContractName", DbType.String, (string)null);
                        foreach (var contractName in contractNames)
                        {
                            cmd.Parameters[1].Value = contractName;
                            await cmd.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                        }
                        cmd.Transaction.Commit();
                    }
                    return subscriptionId.Value;
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
                    cmd.CommandText = @"SELECT TOP 1 Id FROM StreamsSubscriptions WHERE Name = @Name";
                    cmd.AddInputParam("@Name", DbType.String, name);
                    var subscriptionId = (int?)cmd.ExecuteScalar();
                    if (subscriptionId == null)
                    {
                        cmd.CommandText = @"INSERT INTO StreamsSubscriptions(Name) OUTPUT Inserted.ID VALUES(@Name);";
                        subscriptionId = (int)cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        cmd.CommandText = @"INSERT INTO StreamsSubscriptionContracts(StreamsSubscriptionId,EventContractName)VALUES(@SubscriptionId,@ContractName)";
                        cmd.AddInputParam("@SubscriptionId", DbType.Int32, subscriptionId);
                        cmd.AddInputParam("@ContractName", DbType.String, (string)null);
                        foreach (var contractName in contractNames)
                        {
                            cmd.Parameters[1].Value = contractName;
                            cmd.ExecuteNonQuery();
                        }
                        cmd.Transaction.Commit();
                    }
                    return subscriptionId.Value;
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

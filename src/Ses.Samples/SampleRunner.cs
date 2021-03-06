﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts;
using Ses.Abstracts.Converters;
using Ses.Abstracts.Subscriptions;
using Ses.Domain;
using Ses.MsSql;
using Ses.Samples.Cart;
using Ses.Samples.Serializers;
using Ses.Samples.Subscriptions;
using Ses.Subscriptions;
using Ses.Subscriptions.MsSql;

namespace Ses.Samples
{
    public class SampleRunner
    {
        private static readonly TransactionOptions options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;

        public async Task Run()
        {
            try
            {
                var store = new EventStoreBuilder()
                    .WithSerializer(new JilSerializer())
                    .WithLogger(new NLogLogger("Ses"))
                    .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                    .WithMsSqlPersistor(connectionString, x =>
                    {
                        x.Destroy(true);
                        x.Initialize();
                        x.RunLinearizer(TimeSpan.FromMilliseconds(20), TimeSpan.FromMinutes(20));
                    })
                    .Build();

                await Task.Delay(1000);

                await Sample1(store);
                await Sample2(store);

                Console.WriteLine(@"Starting subscriptions");
                var subs = SampleSubscriptions();
                await subs.StartAsync();

                foreach (var poller in subs.GetPollers())
                {
                    Console.WriteLine(poller);
                    foreach (var state in poller.SourceSequenceInfo)
                    {
                        Console.WriteLine($@"\t{state}");
                    }
                }

                await Task.Delay(5000);
                Console.WriteLine(@"Stopping subscriptions");
                store.Dispose();
                subs.Dispose();
                Console.WriteLine(@"Starting perf test");
                await SamplePerfTest();

                Console.WriteLine(@"Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }

        private static async Task Sample1(IEventStore store)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled))
            {
                var streamId = SequentialGuid.NewGuid();
                var aggregate = new ShoppingCart(streamId, Guid.Empty);
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);

                var commitId = SequentialGuid.NewGuid();
                var stream = new EventStream(commitId, aggregate.TakeUncommittedEvents());

                // Adding metadata item (key, value)
                stream.Metadata = new Dictionary<string, object>
                {
                    { "RequestIP", "0.0.0.0" },
                    { "User", "John Doe" }
                };

                await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

                await store.LoadAsync(streamId, 1, false);
                scope.Complete();
            }
        }

        private static async Task Sample2(IEventStore store)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled))
            {
                var repo = new Repository<ShoppingCart>(store);
                var streamId = SequentialGuid.NewGuid();
                var aggregate = new ShoppingCart(streamId, SequentialGuid.NewGuid());
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);
                var item2Id = SequentialGuid.NewGuid();
                aggregate.AddItem(item2Id, name: "Product 2", quantity: 1);
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 3", quantity: 5);
                aggregate.RemoveItem(item2Id);
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 4", quantity: 1);

                var snap = aggregate.GetSnapshot();

                await repo.SaveChangesAsync(aggregate);
                await store.Advanced.UpdateSnapshotAsync(aggregate.Id, snap.Version, snap.State);
                aggregate = await repo.LoadAsync(streamId);
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 5", quantity: 5);
                await repo.SaveChangesAsync(aggregate);

                aggregate = await repo.LoadAsync(streamId);
                Console.WriteLine($@"Aggregate expected version 7 = {aggregate.CommittedVersion}");

                scope.Complete();
            }
        }

        private static async Task SamplePerfTest()
        {
            using (var store = new EventStoreBuilder()
                .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                .WithMsSqlPersistor(connectionString, c => c.RunLinearizer(TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(240)))
                .WithSerializer(new JilSerializer())
                .Build())
            {

                const int count = 10000;
                var tasks = new List<Task>(count);
                var token = new System.Threading.CancellationToken();
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < count; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled))
                        {
                            var streamId = SequentialGuid.NewGuid();
                            var aggregate = new ShoppingCart(streamId, Guid.Empty);
                            aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);

                            var commitId = SequentialGuid.NewGuid();
                            var stream = new EventStream(commitId, aggregate.TakeUncommittedEvents());

                            await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream, token);

                            scope.Complete();
                        }
                    }, token);
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
                sw.Stop();
                Console.WriteLine($@"Overall time {sw.ElapsedMilliseconds}ms - {(count / sw.Elapsed.TotalSeconds)}");
                Console.WriteLine(@"Waiting for Linearizer...");
                await Task.Delay(10000, token);
                Console.WriteLine(@"Done.");
            }
        }

        private static EventStoreSubscriptions SampleSubscriptions()
        {
            var sources = new ISubscriptionEventSource[]
            {
                new MsSqlEventSource(new JilSerializer(), connectionString, 1000),
                // new SomeApiEventSource()
            };

            return new EventStoreSubscriptions(new MsSqlPollerStateRepository(connectionString).Destroy(true).Initialize())
                .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly, typeof(MsSqlEventSource).Assembly)
                .WithLogger(new NLogLogger("Ses.Subscriptions"))
                .WithUpConverterFactory(new DefaultUpConverterFactory(typeof(SampleRunner).Assembly))
                .Add(new ProjectionsSubscriptionPoller(sources))
                .Add(new ProcessManagersSubscriptionPoller(sources))
                .Add(new EmailSenderSubscriptionPoller(sources));
        }
    }
}
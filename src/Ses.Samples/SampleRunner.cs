using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts;
using Ses.Abstracts.Logging;
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
                    .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                    .WithMsSqlPersistor(connectionString, x =>
                    {
                        x.Destroy(true);
                        x.Initialize();
                        x.RunLinearizer(TimeSpan.FromMilliseconds(20));
                    })
                    .WithSerializer(new JilSerializer())
                    .Build();

                await Sample1(store);
                //await Sample2(store);
                await SampleSubscriptions();

                //await SamplePerfTest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine(@"Press any key to exit...");
            Console.ReadKey();
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

                await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

                await store.Load(streamId, false);
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

                await repo.SaveChanges(aggregate);
                await store.Advanced.UpdateSnapshot(aggregate.Id, snap.Version, snap.State);
                aggregate = await repo.Load(streamId);
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 5", quantity: 5);
                await repo.SaveChanges(aggregate);

                aggregate = await repo.Load(streamId);
                Console.WriteLine($"Aggregate expected version 7 = {aggregate.CommittedVersion}");

                scope.Complete();
            }
        }

        private static Task SamplePerfTest()
        {
            var store = new EventStoreBuilder()
                .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                .WithMsSqlPersistor(connectionString, x =>
                {
                    x.Destroy(true);
                    x.Initialize();
                })
                .WithSerializer(new JilSerializer())
                .Build();

            const int count = 20000;
            var tasks = new List<Task>(count);
            var token = new System.Threading.CancellationToken();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var task = Task.Run(async () =>
                {
                    var streamId = SequentialGuid.NewGuid();
                    var aggregate = new ShoppingCart(streamId, Guid.Empty);
                    aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);
                    //aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 2", quantity: 2);
                    //aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);

                    var commitId = SequentialGuid.NewGuid();
                    var stream = new EventStream(commitId, aggregate.TakeUncommittedEvents());

                    await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream, token);
                }, token);
                tasks.Add(task);
            }
            sw.Stop();
            Console.WriteLine($"Build tasks time {sw.ElapsedMilliseconds}ms");
            sw.Start();
            Task.WaitAll(tasks.ToArray(), token);
            sw.Stop();
            Console.WriteLine($"Overall time {sw.ElapsedMilliseconds}ms - {(count / sw.Elapsed.TotalSeconds)}");
            return Task.FromResult(0);
        }

        private static async Task SampleSubscriptions()
        {
            var sources = new ISubscriptionEventSource[]
            {
                new MsSqlEventSource(new JilSerializer(), connectionString),
                // new SomeApiEventSource()
            };

            await new EventStoreSubscriber(new MsSqlPoolerStateRepository(connectionString).Initialize())
                .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly, typeof(MsSqlEventSource).Assembly)
                .WithLogger(new DebugLogger())
                .Add(new ProjectionsSubscriptionPooler(sources))
                //.Add(new ProcessManagersSubscriptionPooler(sources))
                .Add(new EmailSenderSubscriptionPooler(sources))
                .Start();
        }
    }
}
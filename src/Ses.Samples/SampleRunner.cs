using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts;
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
                //using (var store = new EventStoreBuilder()
                //    .WithLogger(new NLogLogger())
                //    .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                //    .WithMsSqlPersistor(connectionString, x =>
                //    {
                //        x.Destroy(true);
                //        x.Initialize();
                //        x.RunLinearizer(TimeSpan.FromMilliseconds(20));
                //    })
                //    .WithSerializer(new JilSerializer())
                //    .Build())
                //{
                //    await Sample1(store);
                //    await Sample2(store);
                //}

                await SamplePerfTest();

                //await SampleSubscriptions();
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

                await store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream);

                await store.LoadAsync(streamId, false);
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
                Console.WriteLine($"Aggregate expected version 7 = {aggregate.CommittedVersion}");

                scope.Complete();
            }
        }

        private static async Task SamplePerfTest()
        {
            using (var store = new EventStoreBuilder()
                .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                .WithMsSqlPersistor(connectionString, x =>
                {
                    x.Destroy(true);
                    x.Initialize();
                })
                .WithSerializer(new JilSerializer())
                .Build())
            {

                const int count = 10000;
                var tasks = new List<Task>(count);
                var token = new System.Threading.CancellationToken();
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < count; i++)
                {
                    var streamId = SequentialGuid.NewGuid();
                    var aggregate = new ShoppingCart(streamId, Guid.Empty);
                    aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);
                    //aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 2", quantity: 2);
                    //aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);

                    var commitId = SequentialGuid.NewGuid();
                    var stream = new EventStream(commitId, aggregate.TakeUncommittedEvents());

                    var task = store.SaveChangesAsync(streamId, ExpectedVersion.NoStream, stream, token);
                    tasks.Add(task);
                }
                sw.Stop();
                Console.WriteLine($"Build tasks time {sw.ElapsedMilliseconds}ms");
                sw.Start();
                await Task.WhenAll(tasks);
                sw.Stop();
                Console.WriteLine($"Overall time {sw.ElapsedMilliseconds}ms - {(count / sw.Elapsed.TotalSeconds)}");
            }
        }

        private static async Task SampleSubscriptions()
        {
            var sources = new ISubscriptionEventSource[]
            {
                new MsSqlEventSource(new JilSerializer(), connectionString),
                // new SomeApiEventSource()
            };

            await new EventStoreSubscriptions(new MsSqlPoolerStateRepository(connectionString).Destroy(true).Initialize())
                .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly, typeof(MsSqlEventSource).Assembly)
                .WithLogger(new NLogLogger())
                .Add(new ProjectionsSubscriptionPooler(sources))
                .Add(new ProcessManagersSubscriptionPooler(sources))
                .Add(new EmailSenderSubscriptionPooler(sources))
                .StartAsync();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts;
using Ses.Domain;
using Ses.Samples.Cart;

namespace Ses.Samples
{
    public class SampleRunner
    {
        private static readonly TransactionOptions options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };

        public async Task Run()
        {
            try
            {
                var store = new EventStoreBuilder()
                    .WithDefaultContractsRegistry(typeof(SampleRunner).Assembly)
                    .WithInMemoryPersistor()
                    .WithSerializer(new JsonNetSerializer())
                    //.WithDefaultConcurrencyConflictResolver(x =>
                    //{
                    //    x.RegisterConflictList(typeof(ShoppingCartCreated), typeof(ItemAddedToShoppingCart));
                    //})
                    .Build();

                await Sample1(store);
                await Sample2(store);

                // await SamplePerfTest(store);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task Sample1(IEventStore store)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {
                var streamId = SequentialGuid.NewGuid();
                var aggregate = new ShoppingCart(streamId, Guid.Empty);
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);

                var commitId = SequentialGuid.NewGuid();
                var stream = new EventStream(commitId);

                // Appending events
                stream.Append(aggregate.TakeUncommittedEvents());

                // Adding metadata item (key, value)
                stream.Metadata.Add("RequestIP", "0.0.0.0");
                stream.Metadata.Add("User", "John Doe");

                await store.SaveChanges(streamId, ExpectedVersion.NoStream, stream);

                scope.Complete();
            }
        }

        private static async Task Sample2(IEventStore store)
        {
            var repo = new Repository<ShoppingCart>(store);
            var streamId = SequentialGuid.NewGuid();
            var aggregate = new ShoppingCart(streamId, SequentialGuid.NewGuid());
            aggregate.AddItem(streamId, name: "Product 1", quantity: 3);

            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {
                await repo.SaveChanges(aggregate);

                scope.Complete();
            }

            aggregate = await repo.Load(streamId);
            Console.WriteLine($"Aggregate version {aggregate.CommittedVersion}");
        }

        private static Task SamplePerfTest(IEventStore store)
        {
            const int count = 200000;
            var tasks = new List<Task>(count);
            var token = new System.Threading.CancellationToken();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var task = Task.Run(async () =>
                {
                    var id = SequentialGuid.NewGuid();
                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options))
                    {
                        var repo = new Repository<ShoppingCart>(store);
                        var aggregate = new ShoppingCart(id, id);
                        aggregate.AddItem(Guid.NewGuid(), "Product 1", 1);
                        aggregate.AddItem(Guid.NewGuid(), "Product 2", 1);
                        await repo.SaveChanges(aggregate, null, token);

                        scope.Complete();
                    }
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
    }
}
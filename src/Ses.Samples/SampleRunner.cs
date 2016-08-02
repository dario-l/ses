using System;
using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts;
using Ses.Domain;

namespace Ses.Samples
{
    public class SampleRunner
    {
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
            var options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {
                var aggregate = new ShoppingCart();
                aggregate.AddItem(SequentialGuid.NewGuid(), name: "Product 1", quantity: 3);

                var commitId = SequentialGuid.NewGuid();
                var stream = new EventStream(commitId);

                // Appending events
                stream.Append(aggregate.TakeUncommittedEvents());

                // Adding metadata item (key, value)
                stream.Metadata.Add("RequestIP", "0.0.0.0");
                stream.Metadata.Add("User", "John Doe");

                var id = SequentialGuid.NewGuid();
                await store.SaveChanges(id, 0, stream);

                scope.Complete();
            }
        }

        private static async Task Sample2(IEventStore store)
        {
            var repo = new Repository<ShoppingCart>(store);
            var aggregate = new ShoppingCart();
            var streamId = SequentialGuid.NewGuid();
            aggregate.AddItem(streamId, name: "Product 1", quantity: 3);

            var options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };
            using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
            {
                await repo.SaveChanges(aggregate);

                scope.Complete();
            }

            aggregate = await repo.Load(streamId);
            Console.WriteLine($"Aggregate version {aggregate.CommittedVersion}");
        }
    }
}
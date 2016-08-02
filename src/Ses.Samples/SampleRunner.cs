using System.Threading.Tasks;
using System.Transactions;
using Ses.Abstracts;

namespace Ses.Samples
{
    public class SampleRunner
    {
        public async Task Run()
        {
            var store = new EventStore(null);

            await Sample1(store);
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
    }
}
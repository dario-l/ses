*Ses is dead. Long live https://github.com/getmanta/manta *


# SimpleEventStore

**This project is still under development (with occasionally breaking changes) but it is used in production already**

SimpleEventStore is a simple event store library for .NET based on rdbms persistance.

##### Main goals

 - Async all the way down (sync methods also available)
 - No external dependencies
 - Pluggable persistance storage
 - Support optimistic (and pesimistic for special cases) concurrency with conflict resolver mechanism
 - Support any kind of serialization through ISerializer interface
 - Support any kind of loggers through ILogger interface
 - Support up-conversion of events/snapshots to the newest version
 - Subscriptions to one or many event stream sources for processmanagers/projections/others (with pluggable stream pollers)
 - Built-in implementation for: MS SQL Server, InMemory
 - Built-in single-writer pattern for MS SQL Server implementation (Linearizer)


##### Basic example of usage

``` c#
var options = new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted};
using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled))
{
    var aggregate = new ShoppingCart();
    aggregate.AddItem(SequentalGuid.NewGuid(), name: "Product 1", quantity: 3);


    var stream = new EventStream(id, aggregate.TakeUncommittedEvents());

    // Adding metadata item (key, value)
    stream.Metadata = new Dictionary<string, object>
    {
        { "RequestIP", "0.0.0.0" },
        { "User", "John Doe" }
    };

    var expectedVersion = aggregate.CommittedVersion + stream.Events.Count;
    await _store.SaveChangesAsync(aggregate.Id, expectedVersion, stream);

    scope.Complete();
}
```

Using repository pattern:
``` c#
// Usually transaction scope will be hidden somewhere in infrastructural part of code
using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled))
{
    using (var repo = new SourcedRepo<ShoppingCart>(store))
    {
        // Open stream and restore aggregate from history
        var aggregate = await repo.LoadAsync(id);

        aggregate.AddItem(Guid.NewGuid(), "Product 1", 3);

        await repo.SaveChangesAsync(aggregate);
    }
    scope.Complete();
}
```

##### It is working on production already

SimpleEventStore has used in commercial project https://timeharmony.pl already.

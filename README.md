# SimpleEventStore

*THIS PROJECT IS HIGHLY EXPERIMENTAL*

SimpleEventStore is a very simple event store library for .NET based on rdbms persistance.

##### Main goals

 - Async all the way down
 - No external dependencies
 - Pluggable persistance storage
 - Support optimistic (and pesimistic for special cases) concurrency with conflict resolver mechanism
 - Support any kind of serialization through ISerializer interface
 - Support any kind of loggers through ILogger interface
 - Support up-conversion of events/snapshots to the newest version
 - Subscribing to event stream for processmanagers and denormalizers with pluggable stream readers


##### Basic example of usage

``` c#
var options = new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted};
using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
{
    var id = SequentalGuid.NewGuid();

    var aggregate = new ShoppingCart();
    aggregate.AddItem(SequentalGuid.NewGuid(), name: "Product 1", quantity: 3);


    var stream = new EventStream(id)

    // Appending events
    stream.Append(aggregate.TakeUncommittedEvents());

    // Adding metadata item (key, value)
    stream.Advanced.AddMetadata("RequestIP", "0.0.0.0");
    stream.Advanced.AddMetadata("User", "John Doe");

    await store.SaveChanges(stream);

    scope.Complete();
}
```

Using repository pattern:
``` c#
// Usually transaction scope will be hidden somewhere in infrastructural part of code
using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
{
    using (var repo = new SourcedRepo<ShoppingCart>(store))
    {
        // Open stream and restore aggregate from history
        var aggregate = await repo.Load(id);

        aggregate.AddItem(Guid.NewGuid(), "Product 1", 3);

        await repo.SaveChanges(aggregate);
    }
    scope.Complete();
}
```

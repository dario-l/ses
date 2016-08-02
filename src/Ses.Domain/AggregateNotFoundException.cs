using System;

namespace Ses.Domain
{
    public class AggregateNotFoundException : Exception
    {
        public Guid Id { get; private set; }
        public Type Type { get; private set; }

        public AggregateNotFoundException(Guid id, Type type)
            : base($"Aggregate '{type.FullName}' with id {id} not found.")
        {
            Id = id;
            Type = type;
        }
    }
}
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;
using Ses.Conflicts;

namespace Ses
{
    public interface IEventStoreSettings
    {
        IEventStreamPersistor Persistor { get; }
        IContractsRegistry ContractsRegistry { get; }
        IUpConverterFactory UpConverterFactory { get; }
        ISerializer Serializer { get; }
        ILogger Logger { get; }
        IConcurrencyConflictResolver ConcurrencyConflictResolver { get; }
        void Validate();
    }
}
using Ses.Abstracts;
using Ses.Abstracts.Converters;

namespace Ses
{
    public interface IEventStoreSettings
    {
        IEventStreamPersistor Persistor { get; }
        IContractsRegistry ContractsRegistry { get; }
        IEventUpConverter UpConverter { get; }
        ISerializer Serializer { get; }
    }
}
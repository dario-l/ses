using Ses.Abstracts;
using Ses.Abstracts.EventConversion;

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
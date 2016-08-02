using Ses.Abstracts;
using Ses.Abstracts.Converters;

namespace Ses
{
    public interface IEventStoreSettings
    {
        IEventStreamPersistor Persistor { get; }
        IContractsRegistry ContractsRegistry { get; }
        IEventConverterFactory UpConverterFactory { get; }
        ISerializer Serializer { get; }
    }
}
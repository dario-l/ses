using System;
using System.Threading.Tasks;

namespace Ses.Abstracts
{
    public interface IEventStore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pessimisticLock"></param>
        /// <returns></returns>
        Task<ReadOnlyEventStream> Load(Guid id, bool pessimisticLock = false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Task SaveChanges(EventStream stream);
    }
}

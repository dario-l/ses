using System;

namespace Ses.MsSql
{
    public interface IMsSqlPersistorBuilder
    {
        void Destroy(bool ignoreErrors = false);
        void Initialize();
        void RunLinearizer(TimeSpan timeout, TimeSpan? durationWork = null);
    }
}
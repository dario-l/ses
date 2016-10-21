using System.Data.SqlLocalDb;
using System.Diagnostics;

namespace Ses.TestBase
{
    public class LocalDbInstanceProvider
    {
        private static readonly object locker = new object();
        private static LocalDbInstanceProvider instance;

        public static LocalDbInstanceProvider Current
        {
            get
            {
                if (instance == null)
                {
                    lock(locker)
                    {
                        if (instance == null)
                        {
                            instance = new LocalDbInstanceProvider();
                        }
                    }
                }
                return instance;
            }
        }

        public ISqlLocalDbInstance Start(string serverInstanceName)
        {
            Debug.WriteLine("Starting localDb server...");
            var localDbProvider = new SqlLocalDbProvider();
            LocalDb = localDbProvider.GetOrCreateInstance(serverInstanceName);
            if (!LocalDb.GetInstanceInfo().IsRunning) LocalDb.Start();
            return LocalDb;
        }

        public void Stop()
        {
            Debug.WriteLine("Stopping localDb server...");
            if (LocalDb.GetInstanceInfo().IsRunning) LocalDb.Stop();
        }

        public ISqlLocalDbInstance LocalDb { get; private set; }
    }
}

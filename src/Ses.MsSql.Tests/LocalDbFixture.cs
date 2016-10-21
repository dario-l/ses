using System;
using System.Data.SqlLocalDb;
using Ses.TestBase;

namespace Ses.MsSql.Tests
{
    public class LocalDbFixture : IDisposable
    {
        public LocalDbFixture()
        {
            // Do "global" initialization here; Only called once.
            LocalDb = LocalDbInstanceProvider.Current.Start(typeof(TestsBase).Assembly.FullName);
        }

        public void Dispose()
        {
            // Do "global" teardown here; Only called once.
            LocalDbInstanceProvider.Current.Stop();
        }

        public ISqlLocalDbInstance LocalDb { get; private set; }
    }
}
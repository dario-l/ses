using System;
using System.Threading.Tasks;
using Ses.Abstracts;
using Ses.TestBase;
using Xunit;

namespace Ses.Subscriptions.MsSql.Tests
{
    [Collection("EventStore collection")]
    public abstract class TestsBase : IDisposable
    {
        private readonly DatabaseInstance _dbInstance;

        protected TestsBase(LocalDbFixture fixture)
        {
            _dbInstance = new DatabaseInstance(fixture.LocalDb);
            ConnectionString = _dbInstance.ConnectionString;
        }

        protected string ConnectionString { get; }

        protected async Task<IEventStore> GetEventStore()
        {
            return await _dbInstance.GetEventStore(new[] { typeof(TestsBase).Assembly });
        }

        public void Dispose()
        {
            _dbInstance.Dispose();
        }
    }
}
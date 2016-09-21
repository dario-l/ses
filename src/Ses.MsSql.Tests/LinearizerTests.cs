using System;
using System.Threading.Tasks;
using Ses.Abstracts.Logging;
using Xunit;

namespace Ses.MsSql.Tests
{
    public class LinearizerTests
    {
        [Fact]
        public async Task Can_stop_and_run_many_times()
        {
            var sub = new Linearizer(null, new NullLogger(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            await Task.Run(() => { sub.Start(); });

            await Task.Delay(3000);

            await Task.Run(() => { sub.Start(); });
            Assert.True(sub.IsRunning);
            await Task.Delay(3000);
            Assert.False(sub.IsRunning);
        }
    }
}

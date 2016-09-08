using System;
using Ses.Subscriptions.Tests.Fakes;
using Xunit;

namespace Ses.Subscriptions.Tests
{
    public class HandlerRegistrarTests
    {
        [Fact]
        public void When_handler_implements_sync_and_async_then_throws()
        {
            Assert.Throws<Exception>(() =>
            {
                new HandlerRegistrar(new[] { typeof(FakeHandlerMixed), typeof(FakeHandler) });
            });
        }

    }
}

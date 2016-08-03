using System;

namespace Ses.InMemory
{
    internal class WrongExpectedVersionException : Exception
    {
        public WrongExpectedVersionException(string message, Exception inner = null)
            : base(message, inner)
        { }
    }
}
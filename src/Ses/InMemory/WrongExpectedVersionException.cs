using System;

namespace Ses.InMemory
{
    public class WrongExpectedVersionException : Exception
    {
        public WrongExpectedVersionException(string message, Exception inner = null)
            : base(message, inner)
        { }
    }
}
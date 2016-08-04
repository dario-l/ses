using System;

namespace Ses
{
    public class WrongExpectedVersionException : Exception
    {
        public WrongExpectedVersionException(string message, Exception inner = null)
            : base(message, inner)
        { }
    }
}
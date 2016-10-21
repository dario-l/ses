using System;

namespace Ses
{
    public class WrongExpectedVersionException : Exception
    {
        public string ConflictedContractName { get; private set; }
        public int ConflictedVersion { get; private set; }

        public WrongExpectedVersionException(string message, Exception inner = null)
            : base(message, inner)
        {
        }

        public WrongExpectedVersionException(string message, int conflictedVersion, string conflictedContractName, Exception inner = null)
            : base(message, inner)
        {
            ConflictedVersion = conflictedVersion;
            ConflictedContractName = conflictedContractName;
        }
    }
}
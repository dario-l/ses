namespace Ses
{
    public static class ExpectedVersion
    {
        private const string any = "Any";
        private const string noStream = "NoStream";

        /// <summary>
        /// This write should not conflict with anything and should always succeed.
        /// </summary>
        public const sbyte Any = -1;

        /// <summary>
        /// The stream being written to should not yet exist. If it does exist treat that as a concurrency problem.
        /// </summary>
        public const sbyte NoStream = 0;

        public static string Parse(int version)
        {
            if (version < Any) return $"Forbidden '{version}'";
            switch (version)
            {
                case Any:
                    return any;
                case NoStream:
                    return noStream;
                default:
                    return version.ToString();
            }
        }
    }
}
namespace Ses
{
    public static class ExpectedVersion
    {
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
                    return "Any";
                case NoStream:
                    return "NoStream";
                default:
                    return version.ToString();
            }
        }
    }
}
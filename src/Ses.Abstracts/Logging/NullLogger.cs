namespace Ses.Abstracts.Logging
{
    public class NullLogger : ILogger
    {
        public void Trace(string message, params object[] args) { }
        public void Debug(string message, params object[] args) { }
        public void Info(string message, params object[] args) { }
        public void Warn(string message, params object[] args) { }
        public void Error(string message, params object[] args) { }
        public void Fatal(string message, params object[] args) { }
    }
}

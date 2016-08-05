namespace Ses.Abstracts.Logging
{
    public class DebugLogger : ILogger
    {
        public void Trace(string message, params object[] args) { System.Diagnostics.Debug.WriteLine(message, args); }
        public void Debug(string message, params object[] args) { System.Diagnostics.Debug.WriteLine(message, args); }
        public void Info(string message, params object[] args) { System.Diagnostics.Debug.WriteLine(message, args); }
        public void Warn(string message, params object[] args) { System.Diagnostics.Debug.WriteLine(message, args); }
        public void Error(string message, params object[] args) { System.Diagnostics.Debug.WriteLine(message, args); }
        public void Fatal(string message, params object[] args) { System.Diagnostics.Debug.WriteLine(message, args); }
    }
}
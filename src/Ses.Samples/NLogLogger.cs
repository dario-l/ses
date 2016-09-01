using NLog;

namespace Ses.Samples
{
    public class NLogLogger : Abstracts.ILogger
    {
        private readonly Logger _logger = LogManager.GetLogger("Ses");

        public void Trace(string message, params object[] args)
        {
            if(_logger.IsTraceEnabled) _logger.Trace(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            if (_logger.IsDebugEnabled) _logger.Debug(message, args);
        }

        public void Info(string message, params object[] args)
        {
            if (_logger.IsInfoEnabled) _logger.Info(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            if (_logger.IsWarnEnabled) _logger.Warn(message, args);
        }

        public void Error(string message, params object[] args)
        {
            if (_logger.IsErrorEnabled) _logger.Error(message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            if (_logger.IsFatalEnabled) _logger.Fatal(message, args);
        }
    }
}
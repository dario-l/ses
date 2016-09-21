using NLog;

namespace Ses.Samples
{
    public class NLogLogger : Abstracts.ILogger
    {
        private readonly Logger _logger;

        public NLogLogger(string name)
        {
            _logger = LogManager.GetLogger(name);
        }

        public void Trace(string message, params object[] args)
        {
            _logger.Trace(message, args);
        }

        public void Trace(string message)
        {
            _logger.Trace(message);
        }

        public void Trace<T1>(string message, T1 arg1)
        {
            _logger.Trace(message, arg1);
        }

        public void Trace<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            _logger.Trace(message, arg1, arg2);
        }

        public void Trace<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            _logger.Trace(message, arg1, arg2, arg3);
        }

        public void Debug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Debug<T1>(string message, T1 arg1)
        {
            _logger.Debug(message, arg1);
        }

        public void Debug<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            _logger.Debug(message, arg1, arg2);
        }

        public void Debug<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            _logger.Debug(message, arg1, arg2, arg3);
        }

        public void Info(string message, params object[] args)
        {
            _logger.Info(message, args);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Info<T1>(string message, T1 arg1)
        {
            _logger.Info(message, arg1);
        }

        public void Info<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            _logger.Info(message, arg1, arg2);
        }

        public void Info<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            _logger.Info(message, arg1, arg2, arg3);
        }

        public void Warn(string message, params object[] args)
        {
            _logger.Warn(message, args);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Warn<T1>(string message, T1 arg1)
        {
            _logger.Warn(message, arg1);
        }

        public void Warn<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            _logger.Warn(message, arg1, arg2);
        }

        public void Warn<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            _logger.Warn(message, arg1, arg2, arg3);
        }

        public void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error<T1>(string message, T1 arg1)
        {
            _logger.Error(message, arg1);
        }

        public void Error<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            _logger.Error(message, arg1, arg2);
        }

        public void Error<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            _logger.Error(message, arg1, arg2, arg3);
        }

        public void Fatal(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }

        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }

        public void Fatal<T1>(string message, T1 arg1)
        {
            _logger.Fatal(message, arg1);
        }

        public void Fatal<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            _logger.Fatal(message, arg1, arg2);
        }

        public void Fatal<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            _logger.Fatal(message, arg1, arg2, arg3);
        }
    }
}
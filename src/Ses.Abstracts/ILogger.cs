namespace Ses.Abstracts
{
    public interface ILogger
    {
        void Trace(string message, params object[] args);
        void Trace(string message);
        void Trace<T1>(string message, T1 arg1);
        void Trace<T1, T2>(string message, T1 arg1, T2 arg2);
        void Trace<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3);

        void Debug(string message, params object[] args);
        void Debug(string message);
        void Debug<T1>(string message, T1 arg1);
        void Debug<T1, T2>(string message, T1 arg1, T2 arg2);
        void Debug<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3);

        void Info(string message, params object[] args);
        void Info(string message);
        void Info<T1>(string message, T1 arg1);
        void Info<T1, T2>(string message, T1 arg1, T2 arg2);
        void Info<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3);

        void Warn(string message, params object[] args);
        void Warn(string message);
        void Warn<T1>(string message, T1 arg1);
        void Warn<T1, T2>(string message, T1 arg1, T2 arg2);
        void Warn<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3);

        void Error(string message, params object[] args);
        void Error(string message);
        void Error<T1>(string message, T1 arg1);
        void Error<T1, T2>(string message, T1 arg1, T2 arg2);
        void Error<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3);

        void Fatal(string message, params object[] args);
        void Fatal(string message);
        void Fatal<T1>(string message, T1 arg1);
        void Fatal<T1, T2>(string message, T1 arg1, T2 arg2);
        void Fatal<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3);
    }
}
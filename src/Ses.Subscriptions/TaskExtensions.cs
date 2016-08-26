using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    internal static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable<T> NotOnCapturedContext<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaitable NotOnCapturedContext(this Task task)
        {
            return task.ConfigureAwait(false);
        }

        public static void SwallowException(this Task task)
        {
            task.ContinueWith(x =>
            {
                if (x.Exception != null) Debug.WriteLine(x.Exception);
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Develore.Windows
{
    public static class CoreExtensions
    {
        public static void Invoke(this SynchronizationContext context, Action action)
        {
            if (SynchronizationContext.Current == context)
            {
                action();
            }
            else
            {
                SendOrPostCallback cb = delegate (object state)
                {
                    action();
                };

                context.Send(cb, null);
            }
        }

        public static T Invoke<T>(this SynchronizationContext context, Func<T> method)
        {
            T result = default(T);

            if (SynchronizationContext.Current == context)
            {
                result = method();
            }
            else
            {
                SendOrPostCallback cb = delegate (object state)
                {
                    result = method();
                };
                context.Send(cb, null);
            }

            return result;
        }

        /// <summary>
        /// Tries to dispose the current object.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the object is a disposable object and if the object was disposed without errors. Returns <c>false</c> otherwise.
        /// </returns>
        public static bool TryDispose(this object obj)
        {
            if (obj is IDisposable)
            {
                try
                {
                    ((IDisposable)obj).Dispose();
                    return true;
                }
                catch { }
            }
            return false;
        }

        public static bool TryInvoke(this SynchronizationContext context, Action action)
        {
            try
            {
                context.Invoke(action);
                return true;
            }
            catch { }

            return false;
        }

        public static bool TryInvoke<T>(this SynchronizationContext context, Func<T> method, out T result)
        {
            result = default(T);
            try
            {
                result = context.Invoke<T>(method);
                return true;
            }
            catch { }

            return false;
        }

    }
}

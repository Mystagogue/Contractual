using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;

namespace Contractual.DomainModel
{
    public class LogicalContext<T> : IDisposable where T : class
    {
        public static string Name = typeof(T).Name;
        public static string Namespace = typeof(T).Namespace;

        private bool disposed;

        LogicalContext(T value, string key = null)
        {
            Name = key ?? Name;
            Set(value, Name);
        }

        /// <summary>
        /// Retrieve the ambient value, if it exists.
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public static T Get(string k = null)
        {
            T retVal = null;
            var key = k ?? Name;
            if (HttpContext.Current != null)
            {
                retVal = HttpContext.Current.Items[key] as T;
            }
            else
            {
                retVal = CallContext.LogicalGetData(key) as T;
            }
            return retVal;
        }

        /// <summary>
        /// Assign the ambient value.
        /// </summary>
        /// <remarks>
        /// Do not modify the properties of the ambient instance after
        /// it is stored in logical context.  This rule ensures that,
        /// in a multi-thread system, one thread does not change the value
        /// seen by another thread. 
        /// </remarks>
        /// <param name="v">A value to remain constant throughout the logical request.</param>
        /// <param name="k">An alternate key name.</param>
        public static void Set(T v, string k = null)
        {
            var key = k ?? Name;
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items[key] = v;
            }
            else
            {
                CallContext.LogicalSetData(key, v);
            }
        }

        /// <summary>
        /// Remove an ambient value, to prevent thread storage bleeding.
        /// </summary>
        /// <param name="k"></param>
        public static void Free(string k = null)
        {
            CallContext.FreeNamedDataSlot(k ?? Name);
        }

        /// <summary>
        /// Remove an ambient value, to prevent thread storage bleeding.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
            }
        }

        public void Dispose(bool disposing)
        {
            disposed = true;
            Free(Name);
            GC.SuppressFinalize(this);
        }
    }
}

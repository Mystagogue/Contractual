using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace Contractual.WCF
{
    public static class WcfProxyExtensions
    {
        /// <summary>
        /// Replacement for the "using" statement.  Offers proper proxy disposal in the event of exceptions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult">Value returned from func param</typeparam>
        /// <param name="client">WCF client proxy</param>
        /// <param name="func">Delegate which calls a method on the proxy</param>
        /// <returns></returns>
        public static TResult Using<T, TResult>(this ClientBase<T> client, Func<T, TResult> func) where T : class
        {
            TResult result = default(TResult);

            try
            {
                result = func(client as T);
            }
            finally
            {
                client.CloseOrAbort();
            }

            return result;
        }

        /// <summary>
        /// Replacement for the "using" statement.  Offers proper proxy disposal in the event of exceptions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client">WCF client proxy</param>
        /// <param name="action">Delegate which calls a method on the proxy</param>
        public static void Using<T>(this ClientBase<T> client, Action<T> action) where T : class
        {
            try
            {
                action(client as T);
            }
            finally
            {
                client.CloseOrAbort();
            }
        }

        /// <summary>
        /// Replacement for calls to Close() or Abort().  Ensures exceptions are handled correctly.
        /// </summary>
        /// <param name="client">WCF client proxy</param>
        public static void CloseOrAbort(this ICommunicationObject client)
        {

            //All the gyrations this function goes through is merely to accomodate bindings that employ
            //transport session.  The obvious example being the NetTcpBinding.  
            //Furthermore, the wsHttpBinding can create a simulated transport session when it is configured for
            //reliable session or a security session.

            //A transport session can fault the proxy before trying to close it or while trying to
            //close it.  Either case will cause the proxy to throw an exception when disposed.

            //The implications of http://msdn.microsoft.com/en-us/library/ms789041(v=vs.100).aspx
            //is to call Abort() when the proxy is Faulted.
            if (client.State == CommunicationState.Faulted)
            {
                client.Abort();
            }
            else
            {
                try
                {
                    //With a transport session, this could throw an exception
                    //if a race-condition caused the "Opened" state to change
                    //to "Faulted" just before the Close() could be called (not likely!).
                    client.Close();
                }
                catch (CommunicationException)
                {
                    client.Abort();
                }
                catch (TimeoutException)
                {
                    client.Abort();
                }
            }
        }
    }

}

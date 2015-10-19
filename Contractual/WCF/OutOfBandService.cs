using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;

//Remoting is *not* used; the CallContext is simply misplaced here.
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace Contractual.WCF
{
    public class OutOfBandService<Context> : ICallContextInitializer
    {
        /// <summary>
        /// A global override for setting context from out-of-band parameters.  The string param matches the templated type argument name, and can be used as a key.
        /// </summary>
        public static Action<string, Context> SetContext { get; set; }
        public static string Identifier
        {
            get
            {
                return typeof(Context).Name;
            }
        }

        public string HeaderName { get; internal set; }
        internal string HeaderNamespace { get; set; }
        private readonly bool requireContext;

        public OutOfBandService(bool requireContext, Action<string, Context> setContext = null)
        {
            var contextType = typeof(Context);
            HeaderName = contextType.Name;
            HeaderNamespace = contextType.Namespace;
            this.requireContext = requireContext;

            SetContext = setContext ?? SetContext ?? DefaultSetContext;
        }

        public static void DefaultSetContext(string k, Context v)
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items[k] = v;
            }
            else
            {
                CallContext.LogicalSetData(k, v);
            }
        }

        public void AfterInvoke(object correlationState)
        {
            CallContext.FreeNamedDataSlot(HeaderName);
        }

        public object BeforeInvoke(System.ServiceModel.InstanceContext instanceContext, System.ServiceModel.IClientChannel channel, System.ServiceModel.Channels.Message message)
        {
            int headerIndex = message.Headers.FindHeader(HeaderName, HeaderNamespace);
            if (headerIndex >= 0)
            {
                Context context = message.Headers.GetHeader<Context>(headerIndex);

                SetContext(HeaderName, context);
            }
            else
            {
                if (requireContext)
                {
                    var key = String.Format("{0}/{1}", HeaderNamespace, HeaderName);
                    var msg = String.Format(OutOfBand.ErrorNoHeaderContext, OutOfBand.ErrorHeading, key);
                    throw new InvalidOperationException(msg);
                }
            }

            //'correlationState' object not needed because there is no "old state" to return to. 
            //http://wcfpro.wordpress.com/2011/01/14/icallcontextinitializer/
            return null;
        }
    }
}

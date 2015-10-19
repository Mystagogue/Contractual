using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

//Remoting is *not* used; the CallContext is simply misplaced here.
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace Contractual.WCF
{
    public class OutOfBandClient<Context> : IClientMessageInspector where Context : class
    {
        /// <summary>
        /// A global override for capturing context to be sent as out-of-band parameters.  The string param matches the return type-name, and can be used as a key.
        /// </summary>
        public static Func<string, Context> GetContext { get; set; }
        public static string Identifier
        {
            get
            {
                return typeof(Context).Name;
            }
        }

        public string HeaderName { get; internal set; }
        internal string HeaderNamespace { get; set; }
        private bool requireContext;

        public OutOfBandClient(bool requireContext, Func<string, Context> getContext)
        {
            var contextType = typeof(Context);

            HeaderName = contextType.Name;
            HeaderNamespace = contextType.Namespace;
            //Identifier = HeaderName;
            this.requireContext = requireContext;

            GetContext = getContext ?? GetContext ?? DefaultGetContext;
        }

        public static Context DefaultGetContext(string k)
        {
            Context retVal = null;
            if (HttpContext.Current != null)
            {
                retVal = HttpContext.Current.Items[k] as Context;
            }
            else
            {
                retVal = CallContext.LogicalGetData(k) as Context;
            }
            return retVal;
        }

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel)
        {
            Context profile = GetContext(HeaderName);

            if (profile != null)
            {
                request.Headers.Add(MessageHeader.CreateHeader(HeaderName, HeaderNamespace, profile));
            }
            else if (requireContext)
            {
                var msg = String.Format(OutOfBand.ErrorNoHeaderContext, OutOfBand.ErrorHeading, HeaderName);
                throw new InvalidOperationException(msg);
            };

            return null;
        }
    }
}

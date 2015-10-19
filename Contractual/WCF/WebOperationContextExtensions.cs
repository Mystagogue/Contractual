using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;
using System.Text;

namespace Contractual.WCF
{
    public static class WebOperationContextExtensions
    {
        public static WebFaultException CreateWebFault(this WebOperationContext context, HttpStatusCode code, string reason)
        {
            context.OutgoingResponse.StatusDescription = reason;
            return new WebFaultException(code);
        }

        public static WebFaultException<T> CreateWebFault<T>(this WebOperationContext context, HttpStatusCode code, string reason, T payload, IEnumerable<Type> knownTypes = null)
        {
            context.OutgoingResponse.StatusDescription = reason;
            if (knownTypes != null)
            {
                return new WebFaultException<T>(payload, code);
            }
            else
            {
                return new WebFaultException<T>(payload, code, knownTypes);
            }
        }

    }
}

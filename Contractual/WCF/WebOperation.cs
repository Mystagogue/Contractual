using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Contractual.WCF
{
    /// <summary>
    /// WCF REST error generation helper functions.
    /// </summary>
    public class WebOperation
    {
        /// <summary>
        /// Convert the WCF REST default 400 "Bad Request" into a 500 "Internal Server Error" for unexpected, unhandled exceptions.
        /// </summary>
        /// <param name="ex"></param>
        public static void ChangeTo500(Exception ex)
        {
            if (!(ex is FaultException))
            {
                OutgoingWebResponseContext response = WebOperationContext.Current.OutgoingResponse;
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                }
            }
        }

        /// <summary>
        /// Wrapper for WCF REST service operations.  Converts an unhandled exception from a 400 to 500 HTTP response code.
        /// </summary>
        /// <param name="operation"></param>
        public static void Execute(Action operation)
        {
            try
            {
                operation();
            }
            catch (Exception ex)
            {
                ChangeTo500(ex);
                throw;
            }
        }

        /// <summary>
        /// Wrapper for WCF REST service operations.  Converts an unhandled exception from a 400 to 500 HTTP response code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static T Execute<T>(Func<T> operation)
        {
            T retVal = default(T);
            try
            {
                retVal = operation();
            }
            catch (Exception ex)
            {
                ChangeTo500(ex);
                throw;
            }
            return retVal;
        }
    }
}

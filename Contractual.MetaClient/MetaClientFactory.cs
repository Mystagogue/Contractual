using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Contractual.MetaClient
{
    /// <summary>
    /// This has the same function as the MS HttpClientFactory, but is capable of creating sub-classes of HttpClient.
    /// </summary>
    public static class SuaveClientFactory
    {
        private static Dictionary<string, JsonMediaTypeFormatter> jsonFormatters = new Dictionary<string, JsonMediaTypeFormatter>()
        {
            {"", new JsonMediaTypeFormatter()},
            {"basicSlim", new JsonMediaTypeFormatter(){
                SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings() {
                   DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                   NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                }
            }}
        };

        public static Dictionary<string, JsonMediaTypeFormatter> JsonFormatters
        {
            get { return jsonFormatters; }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Serve.Shared.Http.SuaveClient" />
        /// </summary>
        /// <typeparam name="T">The concrete type of SuaveClient to create</typeparam>
        /// <param name="handlers">The list of HTTP handler that delegates the processing of HTTP response messages to another handler.</param>
        /// <returns>T</returns>
        public static T Create<T>(params DelegatingHandler[] handlers) where T : SuaveClient
        {
            return SuaveClientFactory.Create<T>(new HttpClientHandler(), handlers);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:Serve.Shared.Http.SuaveClient" />
        /// </summary>
        /// <typeparam name="T">The concrete type of SuaveClient to create</typeparam>
        /// <param name="innerHandler">The inner handler which is responsible for processing the HTTP response messages.</param>
        /// <param name="handlers">The list of HTTP handler that delegates the processing of HTTP response messages to another handler.</param>
        /// <returns>T</returns>
        public static T Create<T>(HttpMessageHandler innerHandler, params DelegatingHandler[] handlers) where T : SuaveClient
        {
            T newClient = (T)Activator.CreateInstance(typeof(T), HttpClientFactory.CreatePipeline(innerHandler, handlers));
            return newClient;
        }

        public static T Create<T>(Host endpoint, HttpMessageHandler innerHandler, params DelegatingHandler[] handlers) where T : SuaveClient
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }
            if (String.IsNullOrWhiteSpace(endpoint.Address))
            {
                throw new ArgumentNullException("endpoint.Address");
            }

            T newClient = (T)Activator.CreateInstance(typeof(T), HttpClientFactory.CreatePipeline(innerHandler, handlers));

            newClient.BaseAddress = new Uri(endpoint.Address);
            newClient.JsonFormatter = JsonFormatters[endpoint.JsonFormatter];

            return newClient;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Contractual.MetaClient
{
    public class RequestFilter : IHandlerParam
    {
        public Action<HttpRequestMessage> Sample;
        public RequestFilter(Action<HttpRequestMessage> sample)
        {
            Sample = sample;
        }
    }

    public class SuaveClient : HttpClient
    {
        private JsonMediaTypeFormatter jsonFormatter = new JsonMediaTypeFormatter();
        public JsonMediaTypeFormatter JsonFormatter
        {
            get { return jsonFormatter; }
            set { jsonFormatter = value; }
        }

        public SuaveClient(HttpMessageHandler handler) : base(handler) { }

        public virtual HttpRequestMessage CreateJsonRequest(object spec, HttpMethod method, string uri, IHandlerParam[] metaData)
        {

            HttpRequestMessage request = null;
            var specType = spec.GetType();

            request = new HttpRequestMessage(method, new Uri(BaseAddress, uri));

            if (method == HttpMethod.Put || method == HttpMethod.Post)
            {
                request.Content = new ObjectContent(specType, spec, jsonFormatter);
            }

            RequestFilter filter = null;
            foreach (var param in metaData)
            {
                Type metaType = param.GetType();
                if (metaType == typeof(RequestFilter))
                {
                    filter = param as RequestFilter;
                }
                else
                {
                    request.Properties.Add(metaType.FullName, param);
                }
            }
            if (filter != null)
            {
                filter.Sample(request);
            }
            return request;
        }

        protected virtual Task<HttpResponseMessage> SendAsJsonAsync(object spec, HttpMethod method, string uri, IHandlerParam[] metaData)
        {
            var option = HttpCompletionOption.ResponseContentRead;
            var cancellation = CancellationToken.None;

            var request = CreateJsonRequest(spec, method, uri, metaData);

            return SendAsync(request, option, cancellation);
        }

        #region GET extensions

        public virtual HttpResponseMessage Get(IGet resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Get, resource.Uri, metaData).Result;
        }

        public virtual HttpResponseMessage<T> Get<T>(IGet<T> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Get, resource.Uri, metaData).Result;
            return new HttpResponseMessage<T>(response);
        }

        public virtual HttpResponseMessage<T, M> Get<T, M>(IGet<T, M> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Get, resource.Uri, metaData).Result;
            return new HttpResponseMessage<T, M>(response);
        }

        public virtual Task<HttpResponseMessage> GetAsync(IGet resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Get, resource.Uri, metaData);
        }

        public virtual Task<HttpResponseMessage<T>> GetAsync<T>(IGet<T> resource, params IHandlerParam[] metaData)
        {
            var response = SendAsJsonAsync(resource, HttpMethod.Get, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<T>>(
                t => new HttpResponseMessage<T>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }

        public virtual Task<HttpResponseMessage<T, M>> GetAsync<T, M>(IGet<T, M> resource, params IHandlerParam[] metaData)
        {
            var response = SendAsJsonAsync(resource, HttpMethod.Get, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<T, M>>(
                t => new HttpResponseMessage<T, M>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }

        #endregion

        #region POST extensions
        public virtual HttpResponseMessage PostAsJson(IPost resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Post, resource.Uri, metaData).Result;
        }
        public virtual HttpResponseMessage<T> PostAsJson<T>(IPost<T> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Post, resource.Uri, metaData).Result;
            return new HttpResponseMessage<T>(response);
        }
        public virtual HttpResponseMessage<T, M> PostAsJson<T, M>(IPost<T, M> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Post, resource.Uri, metaData).Result;
            return new HttpResponseMessage<T, M>(response);
        }

        public virtual Task<HttpResponseMessage> PostAsJsonAsync(IPost resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Post, resource.Uri, metaData);
        }
        public virtual Task<HttpResponseMessage<T>> PostAsJsonAsync<T>(IPost<T> resource, params IHandlerParam[] metaData)
        {
            Task<HttpResponseMessage> response = SendAsJsonAsync(resource, HttpMethod.Post, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<T>>(
                t => new HttpResponseMessage<T>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }
        public virtual Task<HttpResponseMessage<T, M>> PostAsJsonAsync<T, M>(IPost<T, M> resource, params IHandlerParam[] metaData)
        {
            Task<HttpResponseMessage> response = SendAsJsonAsync(resource, HttpMethod.Post, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<T, M>>(
                t => new HttpResponseMessage<T, M>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }

        /// <summary>
        /// Send a messgae to a multiplexed endpoint, such as the EventBroker.
        /// </summary>
        /// <remarks>
        /// This method should be used with caution.  There is no need for an IVerb interface on payload, 
        /// but this means that the multiplexed-endpoint (e.g. EventBroker) cannot be readily swapped out for
        /// a regular endpoint.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="value"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>

        public virtual Task<HttpResponseMessage> PostAsJsonAsync(object value, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(value, HttpMethod.Post, null, metaData);
        }
        #endregion

        #region PUT extensions
        public virtual HttpResponseMessage PutAsJson(IPut resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Put, resource.Uri, metaData).Result;
        }
        public virtual HttpResponseMessage<T> PutAsJson<T>(IPut<T> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Put, resource.Uri, metaData).Result;
            return new HttpResponseMessage<T>(response);
        }
        public virtual HttpResponseMessage<T, M> PutAsJson<T, M>(IPut<T, M> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Put, resource.Uri, metaData).Result;
            return new HttpResponseMessage<T, M>(response);
        }

        public virtual Task<HttpResponseMessage> PutAsJsonAsync(IPut resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Put, resource.Uri, metaData);
        }
        public virtual Task<HttpResponseMessage<T>> PutAsJsonAsync<T>(IPut<T> resource, params IHandlerParam[] metaData)
        {
            Task<HttpResponseMessage> response = SendAsJsonAsync(resource, HttpMethod.Put, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<T>>(
                t => new HttpResponseMessage<T>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }
        public virtual Task<HttpResponseMessage<T, M>> PutAsJsonAsync<T, M>(IPut<T, M> resource, params IHandlerParam[] metaData)
        {
            Task<HttpResponseMessage> response = SendAsJsonAsync(resource, HttpMethod.Put, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<T, M>>(
                t => new HttpResponseMessage<T, M>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }
        #endregion

        #region DELETE extensions
        public virtual HttpResponseMessage Delete(IDelete resource, params IHandlerParam[] metaData)
        {
            return SendAsJsonAsync(resource, HttpMethod.Delete, resource.Uri, metaData).Result;
        }
        public virtual HttpResponseMessage<Empty, M> Delete<M>(IDelete<M> resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Delete, resource.Uri, metaData).Result;
            return new HttpResponseMessage<Empty, M>(response);
        }

        public virtual Task<HttpResponseMessage> DeleteAsync(IDelete resource, params IHandlerParam[] metaData)
        {
            HttpResponseMessage response = SendAsJsonAsync(resource, HttpMethod.Delete, resource.Uri, metaData).Result;
            return DeleteAsync(resource.Uri);
        }

        public virtual Task<HttpResponseMessage<Empty, M>> DeleteAsync<M>(IDelete<M> resource, params IHandlerParam[] metaData)
        {
            Task<HttpResponseMessage> response = SendAsJsonAsync(resource, HttpMethod.Delete, resource.Uri, metaData);
            return response.ContinueWith<HttpResponseMessage<Empty, M>>(
                t => new HttpResponseMessage<Empty, M>(t.Result),
                TaskContinuationOptions.ExecuteSynchronously
                );
        }
        #endregion
    }

}

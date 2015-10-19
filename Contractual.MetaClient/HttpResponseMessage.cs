using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Contractual.MetaClient
{
    public class HttpResponseMessage<T> : IDisposable
    {
        protected HttpResponseMessage msg;
        public HttpResponseMessage(HttpResponseMessage msg)
        {
            this.msg = msg;
        }

        public HttpContent Content { get { return msg.Content; } }
        public HttpResponseHeaders Headers { get { return msg.Headers; } }
        public bool IsSuccessStatusCode { get { return msg.IsSuccessStatusCode; } }
        public string ReasonPhrase { get { return msg.ReasonPhrase; } }
        public HttpRequestMessage RequestMessage { get { return msg.RequestMessage; } }
        public HttpStatusCode StatusCode { get { return msg.StatusCode; } }
        public Version Version { get { return msg.Version; } }
        public void Dispose() { msg.Dispose(); msg = null; }
        public HttpResponseMessage<T> EnsureSuccessStatusCode()
        {
            msg.EnsureSuccessStatusCode();
            return this;
        }

        public HttpResponseMessage GetBaseMessage()
        {
            return msg;
        }

        public override string ToString()
        {
            return msg.ToString();
        }

        //public bool TryGetContentValue(out T value)
        //{
        //    return msg.TryGetContentValue(out value);
        //}

        public Task<T> ReadAsAsync()
        {
            var payload = msg.Content.ReadAsAsync<T>();
            return payload;
        }

        //Unfortunately the HttpResponseMessage<T> cannot derive from HttpResponseMessage,
        //but we can at least reduce the pain by offering implicit conversion.
        public static implicit operator HttpResponseMessage(HttpResponseMessage<T> e)
        {
            return e.GetBaseMessage();
        }
    }

    public class HttpResponseMessage<T, M> : HttpResponseMessage<T>
    {
        public HttpResponseMessage(HttpResponseMessage msg) : base(msg) { }

        public M GetMetaValue()
        {
            var payload = msg.Content.ReadAsAsync<M>().Result;
            return payload;
        }
    }

}

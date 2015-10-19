using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
//using Serve.API.Sdk.Client;
//using Serve.API.Sdk.Parameters;
using Contractual.DomainModel;

namespace Serve.Shared.Http
{
    /// <summary>
    /// Provide OAuth headers
    /// </summary>
    /// <remarks>
    /// This code is using Serve.API.Sdk / ServeApiClient, but should instead be using
    /// what is shown here:
    /// http://blogs.msdn.com/b/henrikn/archive/2012/02/25/10268797.aspx
    /// </remarks>
    public class OAuth1MessageSigner : DelegatingHandler
    {
        private static string _consumerKey;
        private static string _consumerSecret;

        private static OAuthBase _oAuthBase = new OAuthBase();

        public OAuth1MessageSigner(string consumerKey, string consumerSecret)
        {
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            OAuthTokenPair tokenPair = null;
            object tokenPairObject;
            string tokenPairName = typeof(OAuthTokenPair).FullName;
            if (request.Properties.TryGetValue(tokenPairName, out tokenPairObject))
            {
                request.Properties.Remove(tokenPairName);
                tokenPair = (OAuthTokenPair)tokenPairObject;
            }

            List<KeyValuePair<string, string>> customPairs = null;
            //This next piece is not standard OAuth.  Unclear what it is for.
            if (tokenPair != null && !String.IsNullOrWhiteSpace(tokenPair.UserAuthFactor))
            {
                customPairs = new List<KeyValuePair<string, string>>();
                customPairs.Add(new KeyValuePair<string, string>("user_auth_factor", tokenPair.UserAuthFactor));
            }

            ObjectContent content = request.Content as ObjectContent;

            // Compute OAuth header 
            string normalizedUri;
            string normalizedParameters;

            string header = _oAuthBase.GenerateOAuthHeader(
                request.RequestUri,
                _consumerKey,
                _consumerSecret,
                tokenPair == null ? null : tokenPair.Token,
                tokenPair == null ? null : tokenPair.TokenSecret,
                request.Method.Method,
                content == null ? new byte[0] : content.ReadAsByteArrayAsync().Result,
                customPairs,
                out normalizedUri,
                out normalizedParameters);

            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", header);

            return base.SendAsync(request, cancellationToken);
        }
    }

}
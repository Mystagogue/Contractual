using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contractual.MetaClient;

namespace Serve.Shared.Http
{
    public class OAuthTokenPair : IHandlerParam
    {
        public string Token { get; set; }
        public string TokenSecret { get; set; }
        public string UserAuthFactor { get; set; }
        public OAuthTokenPair() { }
        public OAuthTokenPair(string token, string tokenSecret)
        {
            Token = token;
            TokenSecret = tokenSecret;
        }
    }
}

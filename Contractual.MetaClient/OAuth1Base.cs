using System;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Serve.Shared.Http
{
    public class OAuth1Base
    {

        /// <summary>
        /// Provides a predefined set of algorithms that are supported officially by the protocol
        /// </summary>
        public enum SignatureTypes
        {
            PLAINTEXT,
            RSASHA1,
            HMACSHA1,
            HMACSHA256,
        }

        /// <summary>
        /// Provides an internal structure to sort the query parameter
        /// </summary>
        protected class QueryParameter
        {
            private string name = null;
            private string value = null;

            public QueryParameter(string name, string value)
            {
                this.name = name;
                this.value = UrlEncode(value);
            }

            public string Name
            {
                get { return name; }
            }

            public string Value
            {
                get { return value; }
            }
        }

        /// <summary>
        /// Comparer class used to perform the sorting of the query parameters
        /// </summary>
        protected class QueryParameterComparer : IComparer<QueryParameter>
        {
            #region IComparer<QueryParameter> Members

            public int Compare(QueryParameter x, QueryParameter y)
            {
                if (x.Name == y.Name)
                {
                    return string.Compare(x.Value, y.Value);
                }
                else
                {
                    return string.Compare(x.Name, y.Name);
                }
            }

            #endregion
        }

        protected const string OAuthVersion = "1.0";
        protected const string OAuthParameterPrefix = "oauth_";

        //
        // List of know and used oauth parameters' names
        //        
        protected const string OAuthConsumerKeyKey = "oauth_consumer_key";
        protected const string OAuthCallbackKey = "oauth_callback";
        protected const string OAuthVersionKey = "oauth_version";
        protected const string OAuthSignatureMethodKey = "oauth_signature_method";
        protected const string OAuthSignatureKey = "oauth_signature";
        protected const string OAuthTimestampKey = "oauth_timestamp";
        protected const string OAuthNonceKey = "oauth_nonce";
        protected const string OAuthTokenKey = "oauth_token";
        protected const string OAuthTokenSecretKey = "oauth_token_secret";
        protected const string OAuthBodyHashKey = "oauth_body_hash";

        protected const string HMACSHA1SignatureType = "HMAC-SHA1";
        protected const string HMACSHA256SignatureType = "HMAC-SHA256";
        protected const string PlainTextSignatureType = "PLAINTEXT";
        protected const string RSASHA1SignatureType = "RSA-SHA1";

        protected Random random = new Random();

        //protected string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };

        /// <summary>
        /// Helper function to compute a hash value
        /// </summary>
        /// <param name="hashAlgorithm">The hashing algoirhtm used. If that algorithm needs some initialization, like HMAC and its derivatives, they should be initialized prior to passing it to this function</param>
        /// <param name="data">The data to hash</param>
        /// <returns>a Base64 string of the hash value</returns>
        private string ComputeHash(HashAlgorithm hashAlgorithm, string data)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException("hashAlgorithm");
            }

            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException("data");
            }

            byte[] dataBuffer = System.Text.Encoding.ASCII.GetBytes(data);
            byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Internal function to cut out all non oauth query string parameters (all parameters not begining with "oauth_")
        /// </summary>
        /// <param name="parameters">The query string part of the Url</param>
        /// <returns>A list of QueryParameter each containing the parameter name and value</returns>
        private void AddQueryParameters(string parameters, List<QueryParameter> result)
        {
            if (parameters.StartsWith("?"))
            {
                parameters = parameters.Remove(0, 1);
            }

            if (!string.IsNullOrEmpty(parameters))
            {
                string[] p = parameters.Split('&');
                foreach (string s in p)
                {
                    if (!string.IsNullOrEmpty(s) && !s.StartsWith(OAuthParameterPrefix))
                    {
                        if (s.IndexOf('=') > -1)
                        {
                            string[] temp = s.Split('=');
                            result.Add(new QueryParameter(temp[0], temp[1]));
                        }
                        else
                        {
                            result.Add(new QueryParameter(s, string.Empty));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is a different Url Encode implementation since the default .NET one outputs the percent encoding in lower case.
        /// While this is not a problem with the percent encoding spec, it is used in upper case throughout OAuth
        /// </summary>
        /// <param name="value">The value to Url encode</param>
        /// <returns>Returns a Url encoded string</returns>
        /// 

        //Brent replaced this method with one provided by Andrew Arnott
        //http://blog.nerdbank.net/2009/05/uriescapedatapath-and.html
        //protected string UrlEncode(string value)
        //{
        //    StringBuilder result = new StringBuilder();
        //
        //    foreach (char symbol in value)
        //    {
        //        if (unreservedChars.IndexOf(symbol) != -1)
        //        {
        //            result.Append(symbol);
        //        }
        //        else
        //        {
        //            result.Append('%' + String.Format("{0:X2}", (int)symbol));
        //        }
        //    }
        //
        //    return result.ToString();
        //}

        /// <summary>
        /// Normalizes the request parameters according to the spec
        /// </summary>
        /// <param name="parameters">The list of parameters already sorted</param>
        /// <returns>a string representing the normalized parameters</returns>
        protected string NormalizeRequestParameters(IEnumerable<QueryParameter> parameters)
        {
            var pairs = parameters.Select(p => String.Format("{0}={1}", p.Name, p.Value));
            return String.Join("&", pairs);
        }

        private List<QueryParameter> GenerateHeaderParameters(string consumerKey, string token, string tokenSecret, string timeStamp, string nonce, string signatureType, string bodyHash, List<KeyValuePair<string, string>> customPairs = null)
        {
            if (token == null)
            {
                token = string.Empty;
            }

            if (tokenSecret == null)
            {
                tokenSecret = string.Empty;
            }

            if (string.IsNullOrEmpty(consumerKey))
            {
                throw new ArgumentNullException("consumerKey");
            }

            if (string.IsNullOrEmpty(signatureType))
            {
                throw new ArgumentNullException("signatureType");
            }

            List<QueryParameter> parameters = new List<QueryParameter>();

            //The order this parameters are added is not required, but it intentionally
            //matches the order seen throughout RFC documents.  This should help
            //during debugging.
            parameters.Add(new QueryParameter(OAuthConsumerKeyKey, consumerKey));
            if (!string.IsNullOrEmpty(token))
            {
                parameters.Add(new QueryParameter(OAuthTokenKey, token));
            }
            parameters.Add(new QueryParameter(OAuthSignatureMethodKey, signatureType));
            parameters.Add(new QueryParameter(OAuthSignatureKey, signatureType));
            parameters.Add(new QueryParameter(OAuthBodyHashKey, bodyHash));
            parameters.Add(new QueryParameter(OAuthTimestampKey, timeStamp));
            parameters.Add(new QueryParameter(OAuthNonceKey, nonce));
            parameters.Add(new QueryParameter(OAuthVersionKey, OAuthVersion));
            if (customPairs != null)
            {
                foreach (var pair in customPairs)
                {
                    parameters.Add(new QueryParameter(pair.Key, pair.Value));
                }
            }

            return parameters;
        }

        /// <summary>
        /// Generate the signature base that is used to produce the signature
        /// </summary>
        /// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
        /// <param name="consumerKey">The consumer key</param>        
        /// <param name="token">The token, if available. If not available pass null or an empty string</param>
        /// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
        /// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
        /// <param name="signatureType">The signature type. To use the default values use <see cref="OAuthBase.SignatureTypes">OAuthBase.SignatureTypes</see>.</param>
        /// <returns>The signature base</returns>
        private string GenerateSignatureBase(Uri url, string httpMethod, List<QueryParameter> parameters, out string normalizedUrl, out string normalizedRequestParameters)
        {
            if (string.IsNullOrEmpty(httpMethod))
            {
                throw new ArgumentNullException("httpMethod");
            }

            AddQueryParameters(url.Query, parameters);

            parameters.Sort(new QueryParameterComparer());

            //Ensures scheme and authority are lower case, and removes standard ports (80/443) if present.
            normalizedUrl = url.GetLeftPart(UriPartial.Path);
            normalizedRequestParameters = NormalizeRequestParameters(parameters.Where(p => p.Name != OAuthSignatureKey));

            StringBuilder signatureBase = new StringBuilder();
            signatureBase.AppendFormat("{0}&", httpMethod.ToUpper());
            signatureBase.AppendFormat("{0}&", UrlEncode(normalizedUrl));
            signatureBase.AppendFormat("{0}", normalizedRequestParameters);

            return signatureBase.ToString();
        }

        /// <summary>
        /// Generate the signature value based on the given signature base and hash algorithm
        /// </summary>
        /// <param name="signatureBase">The signature based as produced by the GenerateSignatureBase method or by any other means</param>
        /// <param name="hash">The hash algorithm used to perform the hashing. If the hashing algorithm requires initialization or a key it should be set prior to calling this method</param>
        /// <returns>A base64 string of the hash value</returns>
        protected string GenerateSignatureUsingHash(string signatureBase, HashAlgorithm hash)
        {
            return ComputeHash(hash, signatureBase);
        }

        /// <summary>
        /// Generates an OAuth header
        /// </summary>
        /// <param name="url">As provided by HttpRequestMessage</param>
        /// <param name="consumerKey">key</param>
        /// <param name="consumerSecret">secret</param>
        /// <param name="token">The token, if available. If not available pass null or an empty string</param>
        /// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
        /// <param name="httpMethod">As provided by the HttpRequestMessage.Method.Method</param>
        /// <param name="body">As provided by HttpRequestMessage.Content.ReadAsByteArrayAsync().Result</param>
        /// <param name="customPairs">Additional header values that are also included in the OAuth signature.  Null if no additions are needed.</param>
        /// <param name="normalizedUrl">For debugging: shows how the Uri was prepped for signing.</param>
        /// <param name="normalizedRequestParameters">For debugging: shows how "base signature" request parameters were formatted.</param>
        /// <returns>The OAuth header</returns>
        public string GenerateOAuthHeader(Uri url, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, byte[] body, List<KeyValuePair<string, string>> customPairs, out string normalizedUrl, out string normalizedRequestParameters)
        {
            return GenerateSignature(url, consumerKey, consumerSecret, token, tokenSecret, httpMethod, GenerateTimeStamp(), GenerateNonce(), SignatureTypes.HMACSHA256, body, customPairs, out normalizedUrl, out normalizedRequestParameters);
        }


        /// <summary>
        /// Generates an OAuth header.
        /// </summary>		
        /// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer seceret</param>
        /// <param name="token">The token, if available. If not available pass null or an empty string</param>
        /// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
        /// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
        /// <param name="timeStamp"></param>
        /// <param name="nonce"></param>
        /// <param name="body">As provided by HttpRequestMessage.Content.ReadAsByteArrayAsync().Result</param>
        /// <param name="customPairs">Additional header values that are also included in the OAuth signature.  Null if no additions are needed.</param>
        /// <param name="normalizedUrl">For debugging: shows how the Uri was prepped for signing.</param>
        /// <param name="normalizedRequestParameters">For debugging: shows how "base signature" request parameters were formatted.</param>
        /// <returns>The OAuth header</returns>
        public string GenerateOAuthHeader(Uri url, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, string timeStamp, string nonce, byte[] body, List<KeyValuePair<string, string>> customPairs, out string normalizedUrl, out string normalizedRequestParameters)
        {
            return GenerateSignature(url, consumerKey, consumerSecret, token, tokenSecret, httpMethod, timeStamp, nonce, SignatureTypes.HMACSHA256, body, customPairs, out normalizedUrl, out normalizedRequestParameters);
        }

        private string CreateHeaderFormat(List<QueryParameter> parameters)
        {
            //The OAuthSignatureKey is not known yet, so a "{0}" is inserted as a placeholder...
            var pairs = parameters.Select(p =>
                String.Format(@"{0}=""{1}""", p.Name, p.Name == OAuthSignatureKey ? "{0}" : p.Value)
            );
            return String.Join(", ", pairs);
        }

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>		
        /// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer seceret</param>
        /// <param name="token">The token, if available. If not available pass null or an empty string</param>
        /// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
        /// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
        /// <param name="timeStamp"></param>
        /// <param name="nonce"></param>
        /// <param name="signatureType">The type of signature to use</param>
        /// <param name="body">As provided by HttpRequestMessage.Content.ReadAsByteArrayAsync().Result</param>
        /// <param name="customPairs">Additional header values that are also included in the OAuth signature.  Null if no additions are needed.</param>
        /// <param name="normalizedUrl">For debugging: shows how the Uri was prepped for signing.</param>
        /// <param name="normalizedRequestParameters">For debugging: shows how "base signature" request parameters were formatted.</param>
        /// <returns>The OAuth header</returns>
        public string GenerateSignature(Uri url, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, string timeStamp, string nonce, SignatureTypes signatureType, byte[] body, List<KeyValuePair<string, string>> customPairs, out string normalizedUrl, out string normalizedRequestParameters)
        {
            normalizedUrl = null;
            normalizedRequestParameters = null;
            HMAC cryptoProvider = null;
            string hashType = null;

            byte[] cryptoHashKey = Encoding.ASCII.GetBytes(string.Format("{0}&{1}", UrlEncode(consumerSecret), string.IsNullOrEmpty(tokenSecret) ? "" : UrlEncode(tokenSecret)));

            switch (signatureType)
            {
                case SignatureTypes.PLAINTEXT:
                    return UrlEncode(string.Format("{0}&{1}", consumerSecret, tokenSecret));
                case SignatureTypes.HMACSHA1:
                    hashType = HMACSHA1SignatureType;
                    cryptoProvider = new HMACSHA1(cryptoHashKey);
                    goto case SignatureTypes.HMACSHA256;
                case SignatureTypes.HMACSHA256:
                    cryptoProvider = cryptoProvider ?? new HMACSHA256(cryptoHashKey);
                    hashType = hashType ?? HMACSHA256SignatureType;

                    string bodyHash;
                    //OAuth_body_hash spec states that regardless of which crypto hash mechanism is used (RSA, HMAC1, HMAC256, etc), a simple SHA1 hash
                    //of the body MUST be used.
                    using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
                    {
                        bodyHash = Convert.ToBase64String(sha1.ComputeHash(body));
                    }

                    var coreParams = GenerateHeaderParameters(consumerKey, token, tokenSecret, timeStamp, nonce, hashType, bodyHash, customPairs);
                    string format = CreateHeaderFormat(coreParams);

                    string signatureBase = GenerateSignatureBase(url, httpMethod, coreParams, out normalizedUrl, out normalizedRequestParameters);

                    string sigHash = GenerateSignatureUsingHash(signatureBase, cryptoProvider);

                    string header = String.Format(format, UrlEncode(sigHash));

                    return header;
                case SignatureTypes.RSASHA1:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException("Unknown signature type", "signatureType");
            }
        }

        /// <summary>
        /// Provide RFC 3986 compliance.
        /// </summary>
        /// <remarks>
        /// This method is purposely avoiding the HttpUtility clas in order to avoid a dependency on the System.Web assembly.
        /// Katana/OWIN projects should not have a System.Web dependency, and this helps to maintain that.  For more information:
        /// http://blog.nerdbank.net/2009/05/uriescapedatapath-and.html
        /// The above link comments:
        /// The <see cref="Uri.EscapeDataString"/> method is <i>supposed</i> to take on
        /// RFC 3986 behavior if certain elements are present in a .config file.  Even if this
        /// actually worked (which in my experiments it <i>doesn't</i>), we can't rely on every
        /// host actually having this configuration element present.
        /// </remarks>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string UrlEncode(string uri)
        {
            // Start with RFC 2396 escaping by calling the .NET method to do the work.
            // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
            // If it does, the escaping we do that follows it will be a no-op since the
            // characters we search for to replace can't possibly exist in the string.
            StringBuilder escaped = new StringBuilder(Uri.EscapeDataString(uri));

            // Upgrade the escaping to RFC 3986, if necessary.
            for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++)
            {
                escaped.Replace(UriRfc3986CharsToEscape[i], Uri.HexEscape(UriRfc3986CharsToEscape[i][0]));
            }

            // Return the fully-RFC3986-escaped string.
            return escaped.ToString();
        }

        /// <summary>
        /// Generate the timestamp for the signature        
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateTimeStamp()
        {
            // Default implementation of UNIX time of the current UTC time
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// Generate a nonce
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return random.Next(123400, 9999999).ToString();
        }

    }
}


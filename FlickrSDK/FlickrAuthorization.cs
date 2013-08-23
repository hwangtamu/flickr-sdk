using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace FlickrSDK
{
    public class FlickrAuthorization
    {
        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }
        public string CallbackUrl { get; private set; }

        private const string RequestTokenUrl = "https://secure.flickr.com/services/oauth/request_token";
        private const string AuthorizeUrl = "https://secure.flickr.com/services/oauth/authorize";
        private const string AccessTokenUrl = "http://www.flickr.com/services/oauth/access_token";

        private string _oAuthToken;
        private string _oAuthTokenSecret;

        public FlickrAuthorization(string key, string secret, string callbackUrl)
        {
            ConsumerKey = key;
            ConsumerSecret = secret;
            CallbackUrl = callbackUrl;
        }

        public async Task InitAsync()
        {
            if (string.IsNullOrEmpty(_oAuthToken))
            {
                await RequestOAuthTokenAsync();
            }

            string autorizeUrl = AuthorizeUrl + "?oauth_token=" + _oAuthToken + "&perms=write";
            var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(autorizeUrl), new Uri(CallbackUrl));
            if (webAuthenticationResult.ResponseStatus != WebAuthenticationStatus.Success)
            {
                throw new Exception(webAuthenticationResult.ResponseData);
            }

            string verifier = ExtractVerifier(webAuthenticationResult.ResponseData);
            await RequestAccessTokenAsync(verifier);
        }

        private async Task RequestOAuthTokenAsync()
        {
            Dictionary<string, string> parameters = CreateOAuthBasicParameters();
            parameters.Add("oauth_callback", Uri.EscapeDataString(CallbackUrl));

            string url = CreateUrlWithSignedParameters("GET", RequestTokenUrl, parameters);

            var response = await new HttpClient().GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }

            string content = await response.Content.ReadAsStringAsync();
            foreach (string value in content.Split('&'))
            {
                string[] splits = value.Split('=');
                switch (splits[0])
                {
                    case "oauth_token":
                        _oAuthToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        _oAuthTokenSecret = splits[1];
                        break;
                }
            }
        }

        private string CreateSignature(string httpMethod, string url, Dictionary<string, string> parameters)
        {
            var sortedParametersStringBuilder = new StringBuilder();

            var sortedKeys = parameters.Keys.OrderBy(x => x);
            foreach (var key in sortedKeys)
            {
                sortedParametersStringBuilder.Append(key);
                sortedParametersStringBuilder.Append("=");
                sortedParametersStringBuilder.Append(parameters[key]);
                sortedParametersStringBuilder.Append("&");
            }

            string sortedParameters = sortedParametersStringBuilder.ToString();
            sortedParameters = sortedParameters.Remove(sortedParameters.Length - 1);

            string baseUrl = string.Format("{0}&{1}&{2}", httpMethod, Uri.EscapeDataString(url), Uri.EscapeDataString(sortedParameters));

            return CreateSignature(baseUrl);
        }

        private string CreateSignature(string baseString)
        {
            IBuffer keyMaterial = CryptographicBuffer.ConvertStringToBinary(ConsumerSecret + "&" + _oAuthTokenSecret, BinaryStringEncoding.Utf8);
            MacAlgorithmProvider hmacSha1Provider = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            CryptographicKey macKey = hmacSha1Provider.CreateKey(keyMaterial);
            IBuffer dataToBeSigned = CryptographicBuffer.ConvertStringToBinary(baseString, BinaryStringEncoding.Utf8);
            IBuffer signatureBuffer = CryptographicEngine.Sign(macKey, dataToBeSigned);

            return CryptographicBuffer.EncodeToBase64String(signatureBuffer);
        }

        public Dictionary<string, string> CreateOAuthBasicParameters()
        {
            var parameters = new Dictionary<string, string>();

            SetOAuthBasicParameters(parameters);

            return parameters;
        }

        private void SetOAuthBasicParameters(Dictionary<string, string> parameters)
        {
            parameters.Add("oauth_consumer_key", ConsumerKey);
            parameters.Add("oauth_nonce", new Random().Next(1000000000).ToString());
            parameters.Add("oauth_signature_method", "HMAC-SHA1");
            parameters.Add("oauth_timestamp", FlickrHelper.GetTimeStamp());
            parameters.Add("oauth_version", "1.0");

            if (!string.IsNullOrEmpty(_oAuthToken))
            {
                parameters.Add("oauth_token", _oAuthToken);
            }
        }

        public string CreateUrlWithSignedParameters(string httpMethod, string url, Dictionary<string, string> parameters)
        {
            string signature = CreateSignature(httpMethod, url, parameters);
            return string.Format("{0}?{1}&oauth_signature={2}", url, parameters.UrlParameters(), Uri.EscapeDataString(signature));
        }

        private async Task RequestAccessTokenAsync(string verifier)
        {
            Dictionary<string, string> parameters = CreateOAuthBasicParameters();
            parameters.Add("oauth_verifier", verifier);

            string url = CreateUrlWithSignedParameters("GET", AccessTokenUrl, parameters);

            var response = await new HttpClient().GetStringAsync(url);
            if (string.IsNullOrEmpty(response))
            {
                throw new Exception();
            }

            foreach (string value in response.Split('&'))
            {
                string[] splits = value.Split('=');
                switch (splits[0])
                {
                    case "oauth_token":
                        _oAuthToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        _oAuthTokenSecret = splits[1];
                        break;
                }
            }
        }

        private string ExtractVerifier(string str)
        {
            string parameters = str.Split('?')[1];

            foreach (string value in parameters.Split('&'))
            {
                string[] splits = value.Split('=');
                switch (splits[0])
                {
                    case "oauth_verifier":
                        return splits[1];
                }
            }

            return null;
        }
    }
}

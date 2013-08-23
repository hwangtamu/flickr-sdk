using System.Collections.Generic;
using System.Text;

namespace FlickrSDK
{
    public static class FlickrExtensions
    {
        public static string UrlParameters(this Dictionary<string, string> parameters)
        {
            var baseString = new StringBuilder();

            foreach (var key in parameters.Keys)
            {
                baseString.Append(key);
                baseString.Append("=");
                baseString.Append(parameters[key]);
                baseString.Append("&");
            }

            string urlParameters = baseString.ToString();

            return urlParameters.Remove(urlParameters.Length - 1);
        }
    }
}
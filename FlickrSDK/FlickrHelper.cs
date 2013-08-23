using System;

namespace FlickrSDK
{
    internal class FlickrHelper
    {
        public static string GetTimeStamp()
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Math.Round(sinceEpoch.TotalSeconds).ToString();
        }
    }
}

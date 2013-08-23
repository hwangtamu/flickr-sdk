using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Popups;

namespace FlickrSDK
{
    public class FlickrApi
    {
        private readonly FlickrAuthorization _authorization;

        private const string RestUrl = "http://ycpi.api.flickr.com/services/rest";
        private const string UploadUrl = "http://up.flickr.com/services/upload/";

        public FlickrApi(string key, string secret, string callbackUrl)
        {
            _authorization = new FlickrAuthorization(key, secret, callbackUrl);
        }

        public async Task InitAsync()
        {
            await _authorization.InitAsync();
        }

        public async Task<string> Login()
        {
            return await CallMethod("flickr.test.login");
        }

        public async Task<string> CallMethod(string method)
        {
            Dictionary<string, string> parameters = _authorization.CreateOAuthBasicParameters();
            parameters.Add("nojsoncallback", "1");
            parameters.Add("format", "json");
            parameters.Add("method", method);

            string url = _authorization.CreateUrlWithSignedParameters("GET", RestUrl, parameters);
            return await new HttpClient().GetStringAsync(url);
        }

        public async Task UploadPhotoAsync(StorageFile storageFile)
        {
            byte[] buffer = (await FileIO.ReadBufferAsync(storageFile)).ToArray();
            await UploadPhotoAsync(buffer, storageFile.Name);
        }

        public async Task UploadPhotoAsync(byte[] photoData, string filename)
        {
            string boundary = "FLICKR_MIME_" + DateTime.Now.ToString("yyyyMMddhhmmss", DateTimeFormatInfo.InvariantInfo);

            Dictionary<string, string> parameters = _authorization.CreateOAuthBasicParameters();

            string url = _authorization.CreateUrlWithSignedParameters("POST", UploadUrl, parameters);

            byte[] buffer = CreateUploadData(photoData, filename, parameters, boundary);

            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));
            var client = new HttpClient();
            await client.PostAsync(url, content);
        }

        private byte[] CreateUploadData(byte[] imageData, string fileName, Dictionary<string, string> parameters, string boundary)
        {
            var keys = new string[parameters.Keys.Count];
            parameters.Keys.CopyTo(keys, 0);
            Array.Sort(keys);

            var contentStringBuilder = new StringBuilder();

            foreach (string key in keys)
            {
                contentStringBuilder.Append("--" + boundary + "\r\n");
                contentStringBuilder.Append("Content-Disposition: form-data; name=\"" + key + "\"\r\n");
                contentStringBuilder.Append("\r\n");
                contentStringBuilder.Append(parameters[key] + "\r\n");
            }

            contentStringBuilder.Append("--" + boundary + "\r\n");
            contentStringBuilder.Append("Content-Disposition: form-data; name=\"photo\"; filename=\"" + Path.GetFileName(fileName) + "\"\r\n");
            contentStringBuilder.Append("Content-Type: image/jpeg\r\n");
            contentStringBuilder.Append("\r\n");

            var encoding = new UTF8Encoding();

            byte[] postContents = encoding.GetBytes(contentStringBuilder.ToString());
            byte[] postFooter = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
            
            var dataBuffer = new byte[postContents.Length + imageData.Length + postFooter.Length];
            Buffer.BlockCopy(postContents, 0, dataBuffer, 0, postContents.Length);
            Buffer.BlockCopy(imageData, 0, dataBuffer, postContents.Length, imageData.Length);
            Buffer.BlockCopy(postFooter, 0, dataBuffer, postContents.Length + imageData.Length, postFooter.Length);

            return dataBuffer;
        }
    }
}

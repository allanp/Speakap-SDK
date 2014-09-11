using System;
using System.IO;
using System.Net;
using System.Text;

namespace Speakap.SDK
{
    /// <summary>
    /// Speakap API wrapper
    ///   You should instantiate the Speakap API as follows:
    /// 
    ///        SpeakapAPI.Speakap speakap = new Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
    /// 
    ///   After you have instantiated the API wrapper, you can perform API calls as follows:
    /// 
    ///   Obviously, MY_APP_ID and MY_APP_SECRET should be replaced with your actual App ID and secret (or by constants containing those).
    /// 
    ///     string response = speakap.Get(string.Format("/networks/{0}/user/{1}/", network_eid, user_eid));
    ///    
    ///     string response = speakap.Post(string.Format("/networks/{0}/messages/", network_eid), @"{
    ///         'body': 'test 123',
    ///         'messageType': 'update',
    ///         'recipient': { 'type': 'network', 'EID': network_eid }
    ///     }");
    ///   
    ///   The response is JSON string result in case of success, it throws a SpeakapAPI.SpeakapApplicationException contains Code and Message in case of error.
    /// </summary>
    /// TODO: Add TLS/SSL certificate support
    public class SpeakapAPI
    {
        #region - Constants -

        private class HTTP
        {
            internal const string DELETE = "DELETE";
            internal const string GET = "GET";
            internal const string POST = "POST";
            internal const string PUT = "PUT";
        }

        #endregion - Constants -

        #region - Properties -

        public string Scheme { get; private set; }

        public string Hostname { get; private set; }

        public string AppId { get; private set; }

        public string AppSecret { get; private set; }

        public string AccessToken { get; private set; }

        public string Accept { get; set; }

        #endregion - Properties -

        #region - Constructors - 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scheme">http or https</param>
        /// <param name="hostname"></param>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        public SpeakapAPI(string scheme, string hostname, string appId, string appSecret)
        {
            Scheme = scheme;
            Hostname = hostname;
            AppId = appId;
            AppSecret = appSecret;
            AccessToken = string.Format("{0}_{1}", AppId, AppSecret);
        }

        #endregion - Constructors -

        #region - Speakap API http://developers.speakap.io/portal/index.html -

        /// <summary>
        /// Performs a DELETE request to the Speakap API
        /// </summary>
        /// <example>
        /// try
        /// {
        ///   SpeakapAPI.Speakap speakap = new SpeakapAPI.Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
        /// 
        ///   string path = string.Format("/networks/{0}/messages/{1}/", network_eid, message_eid);
        /// 
        ///   string response = speakap.Delete(path);
        /// 
        ///   ... do something with response ...
        /// }
        /// catch(SpeakapApplicationException ex)
        /// {
        ///   ... do something with error ...
        /// }
        /// </example>
        /// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
        /// <returns>A JSON string in case of success.</returns>
        /// <exception cref="SpeakapAPI.SpeakapApplicationException"></exception>
        public string Delete(string path)
        {
            string result = null;
            var status = Request(HTTP.DELETE, path, null, out result);
            return HandleResponse(status, result);
        }

        /// <summary>
        /// Performs a GET request to the Speakap API
        /// </summary>
        /// <example>
        /// try
        /// {
        ///   SpeakapAPI.Speakap speakap = new SpeakapAPI.Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
        /// 
        ///   string path = string.Format("/networks/{0}/timeline/embed=messages.author", network_eid);
        /// 
        ///   string response = speakap.Get(path);
        /// 
        ///   ... do something with response ...
        /// }
        /// catch(SpeakapApplicationException ex)
        /// {
        ///   ... do something with error ...
        /// }
        /// </example>
        /// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
        /// <returns>A JSON string in case of success.</returns>
        /// <exception cref="SpeakapAPI.SpeakapApplicationException"></exception>
        public string Get(string path)
        {
            string result = null;
            var status = Request(HTTP.GET, path, null, out result);
            return HandleResponse(status, result);
        }

        /// <summary>
        /// Performs a POST request to the Speakap API
        /// </summary>
        /// <example>
        /// try
        /// {
        ///   SpeakapAPI.Speakap speakap = new SpeakapAPI.Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
        /// 
        ///   string path = string.Format("/networks/{0}/messages/", network_eid);
        ///   string data = @"{""body"": ""test 123"",""messageType"": ""update"",""recipient"": { ""type"": ""network"", ""EID"": network_eid }}"
        /// 
        ///   string response = speakap.Post(path, data);
        /// 
        ///   ... do something with response ...
        /// }
        /// catch(SpeakapApplicationException ex)
        /// {
        ///   ... do something with error ...
        /// }
        /// </example>
        /// <remarks>
        /// If you want to make a POST request to an action (generally all REST endpoints
        /// without trailing slash), you should use the post_action() method instead, as this will use
        /// the proper formatting for the POST data.
        /// </remarks>
        /// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
        /// <param name="data">Object representing the JSON object to submit.</param>
        /// <returns>A JSON string in case of success.</returns>
        /// <exception cref="SpeakapAPI.SpeakapApplicationException"></exception>
        public string Post(string path, string data)
        {
            string result = null;
            var status = Request(HTTP.POST, path, data, out result);
            return HandleResponse(status, result);
        }

        /// <summary>
        /// Performs a POST request to an action endpoint in the Speakap API.
        /// </summary>
        /// <example>
        /// try
        /// {
        ///   SpeakapAPI.Speakap speakap = new SpeakapAPI.Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
        /// 
        ///   string path = string.Format("/networks/{0}/messages/{1}/markread", network_eid, message_eid);
        ///   string data = null;
        /// 
        ///   string response = speakap.PostAction(path, data);
        ///   // or
        ///   string response = speakap.PostAction(path);
        /// 
        ///   ... do something with response ...
        /// }
        /// catch(SpeakapApplicationException ex)
        /// {
        ///   ... do something with error ...
        /// }
        /// </example>
        /// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
        /// <param name="data">Optional object containing the form parameters to submit.</param>
        /// <returns>A JSON string in case of success.</returns>
        /// <exception cref="SpeakapAPI.SpeakapApplicationException"></exception>
        public string PostAction(string path, string data = null)
        {
            string result = null;
            if (!string.IsNullOrEmpty(data))
                data = Uri.EscapeDataString(data);
            var status = Request(HTTP.POST, path, data, out result);
            return HandleResponse(status, result);
        }

        /// <summary>
        /// Performs a PUT request to the Speakap API.
        /// </summary>
        /// <example>
        /// try
        /// {
        ///   SpeakapAPI.Speakap speakap = new SpeakapAPI.Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
        /// 
        ///   string path = string.Format("/networks/{0}/messages/{1}", network_eid, message_eid);
        ///   string data = @"{""messageType"":""comment"",""body"":""some comment update"",""parent"":{""EID"":""123456ABCDEF"",""type"":""update""}}";
        /// 
        ///   string response = speakap.Put(path, data);
        /// 
        ///   ... do something with json_result ...
        /// }
        /// catch(SpeakapApplicationException ex)
        /// {
        ///       ... do something with error ...
        /// }
        /// </example>
        /// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
        /// <param name="data">Optional object containing the form parameters to submit.</param>
        /// <returns>A JSON string in case of success.</returns>
        /// <exception cref="SpeakapAPI.SpeakapApplicationException"></exception>
        public string Put(string path, string data)
        {
            string result = null;
            var status = Request(HTTP.PUT, path, data, out result);
            return HandleResponse(status, result);
        }

        private HttpStatusCode Request(string method, string path, string data, out string result)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var uri = new UriBuilder { Scheme = Scheme, Host = Hostname, Path = path };
            
            if (uri == null || uri.Uri == null)
                throw new InvalidOperationException("uri cannot be null.");

            var httpWebReq = (HttpWebRequest)WebRequest.Create(uri.Uri);

            httpWebReq.Headers.Set(HttpRequestHeader.Authorization, string.Format("Bearer {0}", AccessToken));

            if(!string.IsNullOrEmpty(Accept))
            {
                httpWebReq.Accept = Accept;
            }

            httpWebReq.Method = method;
            httpWebReq.Host = Hostname;
            httpWebReq.ContentType = "charset=utf-8";
            httpWebReq.KeepAlive = false;

            if (!string.IsNullOrEmpty(data))
            {
                var buffer = Encoding.UTF8.GetBytes(data);

                httpWebReq.ContentLength = buffer.Length;
                using (var requestStream = httpWebReq.GetRequestStream())
                {
                    requestStream.Write(buffer, 0, buffer.Length);
                }
            }
            try
            {
                var httpWebResp = (HttpWebResponse)httpWebReq.GetResponse();
                
                using (var stream = httpWebResp.GetResponseStream())
                {
                    using (var sr = new StreamReader(stream, Encoding.UTF8))
                    {
                        result = sr.ReadToEnd();
                    }
                }

                return httpWebResp.StatusCode;
            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }

            return HttpStatusCode.BadRequest;
        }

        private static string HandleResponse(HttpStatusCode status, string data)
        {
            if ((int)status >= 200 && (int)status < 300)
                return data;

            throw new SpeakapApplicationException(-1001, string.Format("Status: {0}, Data: {1}", status, data));
        }

        #endregion - Speakap API -
        
    }
}

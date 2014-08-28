using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SpeakapAPI
{
	/// <summary>
	/// Speakap API wrapper
	///   You should instantiate the Speakap API as follows:
	/// 
	///		SpeakapAPI.Speakap speakap = new Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
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
	public class Speakap
	{
		#region - Constants -

		private const string DELETE = "DELETE";
		private const string GET = "GET";
		private const string POST = "POST";
		private const string PUT = "PUT";

		private const int DefaultSignatureWindowSize = 1; // minute

		#endregion - Constants -

		#region - Properties -

		public string Scheme { get; private set; }

		public string Hostname { get; private set; }

		public string AppId { get; private set; }

		public string AppSecret { get; private set; }

		public string AccessToken { get; private set; }

		public int SignatureWindowSize { get; private set; }

		public string AuthenticatorUri { get; private set; }

		public string Accept { get; set; }

		#endregion - Properties -

		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme">Scheme, default: "https"</param>
		/// <param name="hostname">Hostname, default: "api.speakap.io"</param>
		/// <param name="appId">MY_APP_ID</param>
		/// <param name="appSecret">MY_APP_SECRET</param>
		public Speakap(string scheme, string hostname, string appId, string appSecret)
			: this(scheme, hostname, appId, appSecret, DefaultSignatureWindowSize)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <param name="hostname"></param>
		/// <param name="appId"></param>
		/// <param name="appSecret"></param>
		/// <param name="authenticatorUri"></param>
		public Speakap(string scheme, string hostname, string appId, string appSecret, string authenticatorUri)
			: this(scheme, hostname, appId, appSecret, DefaultSignatureWindowSize, authenticatorUri)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme">Scheme, default: "https"</param>
		/// <param name="hostname">Hostname, default: "api.speakap.io"</param>
		/// <param name="appId">MY_APP_ID</param>
		/// <param name="appSecret">MY_APP_SECRET</param>
		/// <param name="signatureWindowSize">SignatureWindowSize in minutes, default is 1 minute</param>
		public Speakap(string scheme, string hostname, string appId, string appSecret, int signatureWindowSize)
			: this(scheme, hostname, appId, appSecret, signatureWindowSize, new Uri(new Uri(hostname.Replace("api.", "authenticator.")), "/oauth/v2/token").AbsoluteUri)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="scheme"></param>
		/// <param name="hostname"></param>
		/// <param name="appId"></param>
		/// <param name="appSecret"></param>
		/// <param name="signatureWindowSize"></param>
		/// <param name="authenticatorUri"></param>
		public Speakap(string scheme, string hostname, string appId, string appSecret, int signatureWindowSize, string authenticatorUri)
		{
			Scheme = scheme;
			Hostname = hostname;
			AppId = appId;
			AppSecret = appSecret;
			SignatureWindowSize = signatureWindowSize;
			AuthenticatorUri = authenticatorUri;
			AccessToken = string.Format("{0}_{1}", AppId, AppSecret);
		}

		#region - SignedRequest http://developers.speakap.io/portal/tutorials/signed_request.html -

		/// <summary>
		/// Generates the signed request string from the parameters.
		/// </summary>
		/// <remarks> 
		/// This method does not calculate a signature; it simply generates the signed request from the parameters including the signature.
		/// </remarks>
		/// <param name="requestParams">Object containing POST parameters passed during the signed request.</param>
		/// <returns>Query string containing the parameters of the signed request.</returns>
		public static string SignedRequest(NameValueCollection requestParams)
		{
			if (requestParams == null)
				throw new ArgumentNullException("requestParams");

			if (requestParams.AllKeys == null)
				throw new ArgumentException("requestParams.AllKeys cannot be null.");

			if (!IsValidRequestParameters(requestParams))
				throw new ArgumentException(string.Format("Required key is missing in requestParams. parameters: {0}", GetParameters(requestParams)));

			var keys = requestParams.AllKeys.TakeWhile(k => k != "signature").OrderBy(k => k).ToList();
			if(requestParams.AllKeys.Contains("signature"))
			{
				keys.Add("signature");
			}

			return string.Join("&", keys.Select(k => string.Format("{0}={1}", HttpUtility.UrlEncode(k), HttpUtility.UrlEncode(requestParams[k]))));
		}

		private static string GetParameters(NameValueCollection requestParams)
		{
			if (requestParams == null)
				return null;

			var sb = new StringBuilder();
			foreach (var p in requestParams.AllKeys)
			{
				sb.AppendFormat("{0}:{1}&", p, requestParams[p].ToString());
			}
			return sb.ToString();
		}

		private static bool IsValidRequestParameters(NameValueCollection requestParams)
		{
			/*** 
			 * TODO: 
			 * 
			 * var defaultKeys = new [] { "appData", "issuedAt", "locale", "networkEID", "userEID", "signature" };
			 * 
			 * return defaultKeys.Length <= requestParams.AllKeys.Intersect(defaultKeys, StringComparer.InvariantCultureIgnoreCase).Count();
			*/

			return true;
		}

		/// <summary>
		/// Validates the signature of a signed request.
		/// </summary>
		/// <param name="requestParams">Object containing POST parameters passed during the signed request.</param>
		/// <exception cref="SpeakapSignatureValidationException">Throws a SpeakapSignatureValidationError if the signature doesn't match or the signed request is expired.</exception>
		public void ValidateSignature(NameValueCollection requestParams)
		{
			ValidateSignature(AppSecret, SignatureWindowSize, requestParams);
		}

		/// <summary>
		/// Validates the signature of a signed request.
		/// </summary>
		/// <param name="appSecret"></param>
		/// <param name="signatureWindowSize"></param>
		/// <param name="requestParams"></param>
		/// <exception cref="SpeakapSignatureValidationException">Throws a SpeakapSignatureValidationError if the signature doesn't match or the signed request is expired.</exception>
		public static void ValidateSignature(string appSecret, int signatureWindowSize, NameValueCollection requestParams)
		{
			if (requestParams == null)
				throw new ArgumentNullException("requestParams");

			if (requestParams.AllKeys == null)
				throw new ArgumentException("requestParams.AllKeys cannot be null.");

			if (string.IsNullOrEmpty(appSecret))
				throw new InvalidOperationException("AppSecret cannot be null.");

			if (!requestParams.AllKeys.Contains("signature"))
				throw new SpeakapSignatureValidationException("Parameters did not include a signature");

			var signature = requestParams["signature"];

			var queryString = SignedRequest(requestParams);
			if (string.IsNullOrEmpty(queryString))
				throw new InvalidOperationException("queryString cannot be null.");

			var hash = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));

			var buffer = Encoding.UTF8.GetBytes(queryString);
			var inArray = hash.ComputeHash(buffer);
			if (inArray == null)
				throw new InvalidOperationException("inArray cannot be null");

			var computedHash = Convert.ToBase64String(inArray);

			if (computedHash != signature)
				throw new SpeakapSignatureValidationException(string.Format("Invalid signature: {0}", queryString));

			var issuedAt = DateTime.Parse(requestParams["issuedAt"], null, System.Globalization.DateTimeStyles.RoundtripKind);
			var expiresAt = issuedAt.AddMinutes(signatureWindowSize);
			if (DateTime.Now > expiresAt)
				throw new SpeakapSignatureValidationException("Expired signature");
		}

		/// <summary>
		/// Get appData from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetAppData(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, "appData");
		}
		
		/// <summary>
		/// Get networkEID from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetNetworkEID(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, "networkEID");
		}
		
		/// <summary>
		/// Get userEID from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetUserEID(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, "userEID");
		}
		
		/// <summary>
		/// Get issuedAt from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static DateTime GetIssuedAt(string signedRequest)
		{
			var dateTimeString = GetValueFromSignedRequest(signedRequest, "issuedAt");
			return DateTime.Parse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind);
		}

		/// <summary>
		/// Get locale from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetLocale(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, "locale");
		}

		private static string GetValueFromSignedRequest(string signedRequest, string key)
		{
			if (string.IsNullOrWhiteSpace(signedRequest))
				return null;

			// appData=&issuedAt=2014-01-01T00%3A00%3A00.000%2B0000&locale=en-US&networkEID=Fake_networkE1d&userEID=Fake_userE1d
			var signedRequests = signedRequest.Split(new[] { "&amp;" }, StringSplitOptions.RemoveEmptyEntries)
											  .Where(p => p.StartsWith(key))
											  .ToDictionary(
												k => k.Substring(0, k.IndexOf('=')),
												v => (v.IndexOf('=') < 0 || v.IndexOf('=') == v.Length) ? "" : v.Substring(v.IndexOf('=') + 1));

			string value = null;
			signedRequests.TryGetValue(key, out value);
			return value;
		}

		#endregion - SignedRequest -

		#region - OAuth 2.0 http://developers.speakap.io/portal/auth/oauth2.html -

		/// <summary>
		/// {
		///    access_token: "Fake_access_token_1234567",
		///    expires_in: 3600,
		///    token_type: "bearer",
		///    scope: null,
		///    refresh_token: "Fake_refresh_token_1234567"
		/// }
		/// </summary>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="clientId"></param>
		/// <param name="clientSecret"></param>
		/// <param name="accessToken"></param>
		/// <param name="refreshToken"></param>
		/// <returns></returns>
		public string AcquireAccessToken(string username, string password, string clientId, string clientSecret, out string accessToken, out string refreshToken)
		{
			var messageBodyJson = string.Format("'grant_type':\"password\",'username':\"{0}\",'password':\"{1}\",'client_id':\"{2}\",'client_secret':\"{3}\"",
												username, password, clientId, clientSecret);

			string result = null;
			var status = OAuth(messageBodyJson, out result);

			accessToken = null;
			refreshToken = null;
			if (status == HttpStatusCode.OK)
			{
				accessToken = GetJsonPropertyValue(result, "access_token");
				refreshToken = GetJsonPropertyValue(result, "refresh_token");
			}
			
			return result;
		}

		/// <summary>
		/// {
		///    access_token: "Fake_access_token_1234567",
		///    expires_in: 3600,
		///    token_type: "bearer",
		///    scope: ""
		/// }
		/// </summary>
		/// <param name="refreshToken"></param>
		/// <param name="clientId"></param>
		/// <param name="clientSecret"></param>
		/// <returns></returns>
		public string RefreshingAccessToken(string refreshToken, string clientId, string clientSecret, out string accessToken)
		{
			var messageBodyJson = string.Format("'grant_type':\"refresh_token\",'refresh_token':\"{0}\",'client_id':\"{1}\",'client_secret':\"{2}\"",
												refreshToken, clientId, clientSecret);


			string result = null;
			var status = OAuth(messageBodyJson, out result);
			
			accessToken = null;
			if (status == HttpStatusCode.OK)
			{
				accessToken = GetJsonPropertyValue(result, "access_token");
			}
			
			return result;
		}

		private HttpStatusCode OAuth(string messageBody, out string result)
		{
			return OAuth(AuthenticatorUri, messageBody, out result);
		}

		private static HttpStatusCode OAuth(string authenticatorUri, string messageBody, out string result)
		{
			var httpWebReq = (HttpWebRequest)WebRequest.Create(authenticatorUri);

			httpWebReq.Method = POST;
			httpWebReq.Host = new Uri(authenticatorUri).Host;
			httpWebReq.ContentType = string.Format("{0}; charset=utf-8", SpeakapRequestHeaderConentTypes.ApplicationXWwwFormUrlencoded);
			httpWebReq.KeepAlive = false;
			httpWebReq.Headers.Add(HttpRequestHeader.Accept, SpeakapRequestHeaderAccept.ApplicationJson);

			if (!string.IsNullOrEmpty(messageBody))
			{
				var buffer = Encoding.UTF8.GetBytes(messageBody);

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
				result = ex.Message;
			}
			return HttpStatusCode.BadRequest;
		}

		private static string GetJsonPropertyValue(string result, string property)
		{
			if (string.IsNullOrEmpty(result))
				return null;

			var index = result.IndexOf(property, StringComparison.InvariantCultureIgnoreCase);
			if (index < 0)
				return null;

			index = result.IndexOf(":", index);
			if (index < 0)
				return null;

			index = result.IndexOf("\"", index);
			if (index < 0)
				return null;

			var endIndex = result.IndexOf("\"", index + 1);
			if (endIndex < 0)
				return null;

			return result.Substring(index + 1, endIndex - index - 1);
		}

		#endregion - OAuth 2.0 -

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
			var status = Request(DELETE, path, null, out result);
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
			var status = Request(GET, path, null, out result);
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
			var status = Request(POST, path, data, out result);
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
				data = HttpUtility.UrlEncode(data);
			var status = Request(POST, path, data, out result);
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
			var status = Request(PUT, path, data, out result);
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
	}

	/// <summary>
	/// 
	/// </summary>
	public static class SpeakapRequestHeaderAccept
	{
		/// <summary>
		/// */*
		/// </summary>
		public const string Any = @"*/*";
		/// <summary>
		/// application/json
		/// </summary>
		public const string ApplicationJson = @"application/json";
		/// <summary>
		/// application/vnd.speakap.api-v1.0.19+json
		/// </summary>
		public const string ApplicationVndSpeakapApi1019Json = @"application/vnd.speakap.api-v1.0.19+json";
		/// <summary>
		/// application/vnd.speakap.api-v1.0.2+json
		/// </summary>
		public const string ApplicationVndSpeakapApi102Json = @"application/vnd.speakap.api-v1.0.2+json";
	}

	/// <summary>
	/// 
	/// </summary>
	public static class SpeakapRequestHeaderConentTypes
	{
		/// <summary>
		/// multipart/form-data
		/// </summary>
		public const string MultipartFormData = @"multipart/form-data";
		/// <summary>
		/// application/json
		/// </summary>
		public const string ApplicationJson = @"application/json";
		/// <summary>
		/// application/x-www-form-urlencoded
		/// </summary>
		public const string ApplicationXWwwFormUrlencoded = @"application/x-www-form-urlencoded";
	}
}

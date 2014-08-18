using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace SpeakapAPI
{
	/// <summary>
	/// Speakap API wrapper
	///   You should instantiate the Speakap API as follows:
	/// 
	///		var SpeakapAPI = new Speakap("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);
	/// 
	///   After you have instantiated the API wrapper, you can perform API calls as follows:
	/// 
	///   Obviously, MY_APP_ID and MY_APP_SECRET should be replaced with your actual App ID and secret (or by constants containing those).
	/// 
	///     SpeakapResponse response = SpeakapAPI.Get("/networks/%s/user/%s/" % (network_eid, user_eid))
	///    
	///     SpeakapResponse response = SpeakapAPI.Post("/networks/%s/messages/" % network_eid, "{
	///         'body': 'test 123',
	///         'messageType': 'update',
	///         'recipient': { 'type': 'network', 'EID': network_eid }
	///     }")
	///   
	///   The JSON result contains the already parsed reply in case of success, but is None in case of an error.
	///   The error variable is None in case of success, but is an object containing code and message properties in case of an error.
	///   
	///   WARNING: If you use this class to make requests on any other platform than Google App Engine,
	///            the SSL certificate of the Speakap API service is not confirmed, leaving you
	///            vulnerable to man-in-the-middle attacks. This is due to a limitation of the SSL
	///            support in the Python framework. You are strongly advised to take your own
	///            precautions to make sure the certificate is valid.
	/// </summary>
	public sealed class Speakap
	{
		#region - Constants -

		private const string DELETE = "DELETE";
		private const string GET = "GET";
		private const string POST = "POST";
		private const string PUT = "PUT";

		private const int DefaultSignatureWindowSize = 1; // minute

		private static readonly JsonSchema SpeakapResponseSchema = JsonSchema.Parse(@"{
'type' :'object',
'properties':{
	'code':{'type':'integer'},
	'message':{'type':'string'}
},
'required': ['code','message']
}");
		#endregion - Constants -

		#region - Properties -

		public string Scheme { get; private set; }

		public string Hostname { get; private set; }

		public string AppId { get; private set; }

		public string AppSecret { get; private set; }

		public string AccessToken { get; private set; }

		public int SignatureWindowSize { get; private set; }

		#endregion - Properties -

		public Speakap(string scheme, string hostname, string appId, string appSecret)
			: this(scheme, hostname, appId, appSecret, DefaultSignatureWindowSize)
		{
		}

		public Speakap(string scheme, string hostname, string appId, string appSecret, int signatureWindowSize)
		{
			Scheme = scheme;
			Hostname = hostname;
			AppId = appId;
			AppSecret = appSecret;
			SignatureWindowSize = signatureWindowSize;
			AccessToken = string.Format("{0}_{1}", AppId, AppSecret);
		}

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

			var hasSignature = false;

			var keys = requestParams.AllKeys.ToList();
			if (keys.Contains("signature"))
			{
				hasSignature = true;
				requestParams.Remove("signature");
			}

			keys.Sort();

			if (hasSignature)
				keys.Add("signature");

			var queryString = string.Join("&", keys.Select(k => string.Format("{0}={1}", HttpUtility.UrlEncode(k), HttpUtility.UrlEncode(requestParams[k]))));

			return queryString;
		}

		/// <summary>
		/// Validates the signature of a signed request.
		/// </summary>
		/// <exception cref="SpeakapAPI.SignatureValidationErrorException">Raises a SignatureValidationError if the signature doesn't match or the signed request is expired.</exception>
		/// <param name="requestParams">Object containing POST parameters passed during the signed request.</param>
		public void ValidateSignature(NameValueCollection requestParams)
		{
			if (requestParams == null)
				throw new ArgumentNullException("requestParams");

			if (requestParams.AllKeys == null)
				throw new ArgumentException("requestParams.AllKeys cannot be null.");

			if (string.IsNullOrEmpty(AppSecret))
				throw new InvalidOperationException("AppSecret cannot be null.");

			if (!requestParams.AllKeys.Contains("signature"))
				throw new SignatureValidationErrorException("Parameters did not include a signature");

			var signature = requestParams["signature"];

			var queryString = SignedRequest(requestParams);
			if (string.IsNullOrEmpty(queryString))
				throw new InvalidOperationException("queryString cannot be null.");

			var hash = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret));

			var buffer = Encoding.UTF8.GetBytes(queryString);
			var inArray = hash.ComputeHash(buffer);
			if (inArray == null)
				throw new InvalidOperationException("inArray cannot be null");

			var computedHash = Convert.ToBase64String(inArray);

			if (computedHash != signature)
				throw new SignatureValidationErrorException(string.Format("Invalid signature: {0}", queryString));

			var issuedAt = DateTime.Parse(requestParams["issuedAt"], null, System.Globalization.DateTimeStyles.RoundtripKind);
			var expiresAt = issuedAt.AddMinutes(SignatureWindowSize);
			if (DateTime.Now > expiresAt)
				throw new SignatureValidationErrorException("Expired signature");
		}

		/// <summary>
		/// Performs a DELETE request to the Speakap API
		/// </summary>
		/// <example>
		///   (json_result, error) = speakap_api.delete("/networks/%s/messages/%s/" % (network_eid, message_eid))
		///   if json_result:
		///       ... do something with json_result ...
		///   else
		///       ... do something with error ...
		/// </example>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse Delete(string path)
		{
			var response = Request(DELETE, path, null, null);
			return HandleResponse(response);
		}

		/// <summary>
		/// Performs a GET request to the Speakap API
		/// </summary>
		/// <example>
		///    (json_result, error) = speakap_api.get("/networks/%s/timeline/?embed=messages.author" % network_eid)
		///    if json_result:
		///        ... do something with json_result ...
		///    else
		///        ... do something with error ...
		/// </example>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse Get(string path)
		{
			var response = Request(GET, path, null, null);
			return HandleResponse(response);
		}

		/// <summary>
		/// Performs a POST request to the Speakap API
		/// </summary>
		/// <example>
		///     (json_result, error) = speakap_api.post("/networks/%s/messages/" % network_eid, {
		///         "body": "test 123",
		///         "messageType": "update",
		///         "recipient": { "type": "network", "EID": network_eid }
		///     })
		///     if json_result:
		///         ... do something with json_result ...
		///     else
		///         ... do something with error ...
		/// </example>
		/// <remarks>
		/// If you want to make a POST request to an action (generally all REST endpoints
		/// without trailing slash), you should use the post_action() method instead, as this will use
		/// the proper formatting for the POST data.
		/// </remarks>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <param name="data">Object representing the JSON object to submit.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse Post(string path, byte[] data)
		{
			return Post(path, data == null ? null : Encoding.UTF8.GetString(data));
		}
		/// <summary>
		/// Performs a POST request to the Speakap API
		/// </summary>
		/// <example>
		///     (json_result, error) = speakap_api.post("/networks/%s/messages/" % network_eid, {
		///         "body": "test 123",
		///         "messageType": "update",
		///         "recipient": { "type": "network", "EID": network_eid }
		///     })
		///     if json_result:
		///         ... do something with json_result ...
		///     else
		///         ... do something with error ...
		/// </example>
		/// <remarks>
		/// If you want to make a POST request to an action (generally all REST endpoints
		/// without trailing slash), you should use the post_action() method instead, as this will use
		/// the proper formatting for the POST data.
		/// </remarks>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <param name="data">Object representing the JSON object to submit.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse Post(string path, string data)
		{
			var response = Request(POST, path, "application/json", data);

			return HandleResponse(response);
		}

		/// <summary>
		/// Performs a POST request to an action endpoint in the Speakap API.
		/// </summary>
		/// <example>
		///     (json_result, error) = speakap_api.post_action("/networks/%s/messages/%s/markread" % (network_eid, message_eid))
		///     if json_result:
		///         ... do something with json_result ...
		///     else
		///         ... do something with error ...
		/// </example>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <param name="data">Optional object containing the form parameters to submit.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse PostAction(string path, NameValueCollection data)
		{
			string dataString = null;

			if (data != null && data.AllKeys != null)
			{
				dataString = string.Join("&", (data.AllKeys.Select(k => string.Format("{0}={1}", HttpUtility.UrlEncode(k), HttpUtility.UrlEncode(data[k])))));
			}

			return PostAction(path, dataString);
		}

		/// <summary>
		/// Performs a POST request to an action endpoint in the Speakap API.
		/// </summary>
		/// <example>
		///     (json_result, error) = speakap_api.post_action("/networks/%s/messages/%s/markread" % (network_eid, message_eid))
		///     if json_result:
		///         ... do something with json_result ...
		///     else
		///         ... do something with error ...
		/// </example>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <param name="data">Optional object containing the form parameters to submit.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse PostAction(string path, string data)
		{
			var response = Request(POST, path, data, "application/x-www-form-urlencoded");
			return HandleResponse(response);
		}

		/// <summary>
		/// Performs a PUT request to the Speakap API.
		/// </summary>
		/// <example>
		///   (json_result, error) = speakap_api.get("/networks/%s/timeline/?embed=messages.author" % network_eid)
		///   if json_result:
		///       ... do something with json_result ...
		///   else
		///       ... do something with error ...
		/// </example>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <param name="data">Optional object containing the form parameters to submit.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse Put(string path, byte[] data)
		{
			return Put(path, data == null || data.Length == 0 ? null : Encoding.UTF8.GetString(data));
		}

		/// <summary>
		/// Performs a PUT request to the Speakap API.
		/// </summary>
		/// <example>
		///   (json_result, error) = speakap_api.get("/networks/%s/timeline/?embed=messages.author" % network_eid)
		///   if json_result:
		///       ... do something with json_result ...
		///   else
		///       ... do something with error ...
		/// </example>
		/// <param name="path">The path of the REST endpoint, including optional query parameters.</param>
		/// <param name="data">Optional object containing the form parameters to submit.</param>
		/// <returns>A SpeakapResponse object containing the parsed JSON reply (in case of success) and an error object (in case of an error).</returns>
		public SpeakapResponse Put(string path, string data)
		{
			var response = Request(PUT, path, data, "application/json");
			return HandleResponse(response);
		}

		private Tuple<HttpStatusCode, string> Request(string method, string path, string data, string contentType)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			var uri = new UriBuilder(Scheme, Hostname, 80, path);
			if (uri == null || uri.Uri == null)
				throw new InvalidOperationException("uri cannot be null.");

			var httpWebReq = (HttpWebRequest)WebRequest.Create(uri.Uri);

			httpWebReq.Headers.Add(HttpRequestHeader.Authorization, string.Format("Bearer {0}", AccessToken));
			httpWebReq.Headers.Add(HttpRequestHeader.Accept, "application/vnd.speakap.api-v1.0.10+json");

			httpWebReq.Method = method;
			httpWebReq.Host = Hostname;
			httpWebReq.ContentType = string.IsNullOrEmpty(contentType) ? "charset=utf-8" : string.Format("{0}; charset=utf-8", contentType);

			httpWebReq.KeepAlive = false;

			if (!string.IsNullOrEmpty(data))
			{
				httpWebReq.ContentLength = data.Length;
				using (var requestStream = httpWebReq.GetRequestStream())
				{
					if (requestStream == null)
						throw new InvalidOperationException("requestStream cannot be null.");

					using (var sw = new StreamWriter(requestStream, Encoding.UTF8))
					{
						sw.Write(data);
					}
				}
			}

			var httpWebResp = (HttpWebResponse)httpWebReq.GetResponse();
			var stream = httpWebResp.GetResponseStream();
			if (stream == null)
				throw new InvalidOperationException("stream cannot be null.");

			using (var sr = new StreamReader(stream, Encoding.UTF8))
			{
				return new Tuple<HttpStatusCode, string>(httpWebResp.StatusCode, sr.ReadToEnd());
			}
		}

		private static SpeakapResponse HandleResponse(Tuple<HttpStatusCode, string> response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			SpeakapResponse result;
			try
			{
				var json = JObject.Parse(response.Item2);
				if (json.IsValid(SpeakapResponseSchema))
				{
					result = new SpeakapResponse
								{
									Status = response.Item1,
									Code = json["code"].ToObject<long>(),
									Message = json["message"].ToObject<string>(),
									Description = string.Empty
								};
				}
				else
					throw new InvalidOperationException(string.Format("Invalid json string: {0}", response.Item2));
			}
			catch (Exception ex)
			{
				result = new SpeakapResponse
							{
								Status = HttpStatusCode.BadRequest,
								Code = -1001,
								Message = "Unexpected Reply",
								Description = ex.ToString()
							};
			}

			return result;
		}
	}
}

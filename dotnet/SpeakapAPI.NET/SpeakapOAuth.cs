using System;
using System.IO;
using System.Net;
using System.Text;

namespace SpeakapAPI
{
	/// <summary>
	/// OAuth 2.0 http://developers.speakap.io/portal/auth/oauth2.html
	/// </summary>
	public class SpeakapOAuth
	{
		private readonly string _clientId;
		private readonly string _clientSecret;

		public SpeakapOAuth(string clientId, string clientSecret)
		{
			_clientId = clientId;
			_clientSecret = clientSecret;
		}

		/// <summary>
		/// authenticator.{0}/oauth/v2/token
		/// </summary>
		public string AuthenticatorUri { get; set; }

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
		/// <param name="accessToken"></param>
		/// <param name="refreshToken"></param>
		/// <returns></returns>
		public string AcquireAccessToken(string username, string password, out string accessToken, out string refreshToken)
		{
			var messageBodyJson = string.Format("'grant_type':\"password\",'username':\"{0}\",'password':\"{1}\",'client_id':\"{2}\",'client_secret':\"{3}\"",
			                                    username, password, _clientId, _clientSecret);

			string result = null;
			var status = Request(messageBodyJson, out result);

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
		public string RefreshingAccessToken(string refreshToken, out string accessToken)
		{
			var messageBodyJson = string.Format("'grant_type':\"refresh_token\",'refresh_token':\"{0}\",'client_id':\"{1}\",'client_secret':\"{2}\"",
			                                    refreshToken, _clientId, _clientSecret);

			string result = null;
			var status = Request(messageBodyJson, out result);

			accessToken = null;
			if (status == HttpStatusCode.OK)
			{
				accessToken = GetJsonPropertyValue(result, "access_token");
			}

			return result;
		}

		private HttpStatusCode Request(string messageBody, out string result)
		{
			return Request(AuthenticatorUri, messageBody, out result);
		}

		private static HttpStatusCode Request(string authenticatorUri, string messageBody, out string result)
		{
			var httpWebReq = (HttpWebRequest)WebRequest.Create(authenticatorUri);

			httpWebReq.Method = "POST";
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
	}
}
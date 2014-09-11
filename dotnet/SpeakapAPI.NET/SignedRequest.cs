using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SpeakapAPI
{
	/// <summary>
	/// http://developers.speakap.io/portal/tutorials/signed_request.html
	/// </summary>
	public class SignedRequest
	{
		/// <summary>
		/// The time window in seconds
		/// </summary>
		private readonly long _signatureWindowSize;

		private readonly HMACSHA256 _hmac;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="appSecret"></param>
		public SignedRequest(string appSecret)
			: this(appSecret, R.DefaultSignatureWindowSize)
		{
		}

		/// <summary>
		/// The time in seconds should be added to the remote server time in order to be synchronized with the Speapap server time. Default is 0.
		/// </summary>
		/// <example>
		/// If Speapap server time is 10:00:00, and the remote server time is 10:00:20, then the value should be -20.
		/// </example>
		public double TimeDiff { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="appSecret"></param>
		/// <param name="signatureWindowSize">Time in seconds that the window is considered valid</param>
		public SignedRequest(string appSecret, long signatureWindowSize)
		{
			if (appSecret == null)
				throw new ArgumentNullException("appSecret");

			_signatureWindowSize = signatureWindowSize;
			_hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));

			TimeDiff = 0;
		}

		/// <summary>
		/// request parameters in dictionary&lt;string,string&gt;
		/// </summary>
		/// <param name="parameters"></param>
		public void ValidateSignature(IDictionary<string, string> parameters, bool isWithinWindow = true)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			if (!parameters.ContainsKey(R.Signature))
				throw new SpeakapSignatureValidationException("Parameters did not include a signature");

			if (!IsValidRequestParameters(parameters))
				throw new SpeakapSignatureValidationException(string.Format("Missing required parameters, got: {0}", GetSignedRequest(parameters)));

			if (parameters[R.Signature] != GetSignatureFromParameters(parameters))
				throw new SpeakapSignatureValidationException(string.Format("Invalid signature."));

			if (!isWithinWindow)
				return;

			var issuedAtUtc = DateTime.Parse(parameters[R.IssuedAt], null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();

			if (!IsWithinWindow(_signatureWindowSize, issuedAtUtc.AddSeconds(-TimeDiff)))
			{
				throw new SpeakapSignatureValidationException("Expired signature.");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="signedRequest"></param>
		public void ValidateSignature(string signedRequest)
		{
			if(signedRequest == null)
				throw new ArgumentNullException("signedRequest");

			var parameters = SignedRequestHelper.GetParametersFromSignedRequest(signedRequest);

			ValidateSignature(parameters, false);
		}


		/// <summary>
		/// Get the encoded value. To be used in e.g. the Speakap JavaScript proxy
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public string GetSignedRequest(IDictionary<string, string> parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			if (parameters.Count == 0)
				throw new ArgumentException("parameters cannot be empty.");

			if (!parameters.ContainsKey(R.Signature))
				parameters.Add(R.Signature, string.Empty);

			parameters[R.Signature] = GetSignatureFromParameters(parameters);

			return ParametersToQueryString(parameters);
		}


		/// <summary>
		/// Get the parameters, including the signature
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public IDictionary<string, string> GetSignedParameters(IDictionary<string, string> parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			if (parameters.Count == 0)
				throw new ArgumentException("parameters cannot be empty.");

			if (!parameters.ContainsKey(R.Signature))
				parameters.Add(R.Signature, string.Empty);
			parameters[R.Signature] = GetSignatureFromParameters(parameters);

			return parameters;
		}

		/// <summary>
		/// Whether or not the request is within a sane window.
		/// </summary>
		/// <remarks>
		/// Check the absolute Time difference to overcome the local machine time is earlier than the Speakap remote server time
		/// </remarks>
		/// <param name="signatureWindowSize">signature window size in seconds</param>
		/// <param name="issuedAtUtc">UTC date time</param>
		/// <returns></returns>
		protected static bool IsWithinWindow(long signatureWindowSize, DateTime issuedAtUtc)
		{
			var diff = (DateTime.UtcNow - issuedAtUtc).TotalSeconds;
			return 0 <= diff && diff <= signatureWindowSize;
		}

		/// <summary>
		/// Sign the remote payload with the local (shared) secret. The result should be identical to the one we got from the server.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		protected string GetSelfSignedRequest(IDictionary<string, string> parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			if (!parameters.ContainsKey(R.Signature))
				parameters.Add(R.Signature, string.Empty);

			parameters[R.Signature] = GetSignatureFromParameters(parameters);

			return ParametersToQueryString(parameters);
		}

		/// <summary>
		/// Generate the signature, based on the request parameters
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		protected string GetSignatureFromParameters(IDictionary<string, string> parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			if (parameters.Count == 0)
				throw new ArgumentException("parameters cannot be empty.");

			var queryString = ParametersToQueryString((from param in parameters
													   where !string.Equals(R.Signature, param.Key)
													   select param).ToDictionary(p => p.Key, p => p.Value));

			if (queryString == null)
				throw new ArgumentException("queryString");

			var inArray = _hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));

			if (inArray == null)
				throw new ArgumentException("inArray");

			return Convert.ToBase64String(inArray);
		}

		/// <summary>
		/// Validate the existence of the payload properties.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		protected bool IsValidRequestParameters(IDictionary<string, string> parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			return R.DefaultKeys.Length <= parameters.Keys.Intersect(R.DefaultKeys, StringComparer.InvariantCultureIgnoreCase).Count();
		}

		/// <summary>
		/// Convert an array to a query-string, RFC3986 encoded
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string ParametersToQueryString(IDictionary<string, string> parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			var queryString = string.Join(R.Separator, from param in parameters
													   where param.Key != R.Signature
													   orderby param.Key
													   select string.Format("{0}={1}", Uri.EscapeDataString(param.Key),
																					   Uri.EscapeDataString(param.Value == null ? string.Empty : param.Value)));

			if (parameters.ContainsKey(R.Signature))
			{
				var signature = string.Concat(R.Signature, "=", Uri.EscapeDataString(parameters[R.Signature]));

				queryString = string.IsNullOrEmpty(queryString) ? signature : string.Concat(queryString, R.Separator, signature);
			}

			return queryString;
		}

	}
}
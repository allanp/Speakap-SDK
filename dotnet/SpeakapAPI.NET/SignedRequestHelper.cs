using System;
using System.Collections.Generic;
using System.Linq;

namespace Speakap.SDK
{
	public static class SignedRequestHelper
	{
		/// <summary>
		/// Get appData from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetAppData(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, R.AppData);
		}

		/// <summary>
		/// Get networkEID from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetNetworkEID(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, R.NetworkEID);
		}

		/// <summary>
		/// Get userEID from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetUserEID(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, R.UserEID);
		}

		/// <summary>
		/// Get issuedAt from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetIssuedAt(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, R.IssuedAt);
		}

		/// <summary>
		/// Get locale from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetLocale(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, R.Locale);
		}

		/// <summary>
		/// Get role from the signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static string GetRole(string signedRequest)
		{
			return GetValueFromSignedRequest(signedRequest, R.Role);
		}

		/// <summary>
		/// Get parameters from a signed request
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <returns></returns>
		public static IDictionary<string, string> GetParametersFromSignedRequest(string signedRequest)
		{
			if (string.IsNullOrEmpty(signedRequest))
				return null;
			var data = signedRequest.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries);

			var parameters = new Dictionary<string, string>();

			foreach (var parameter in data)
			{
				var pair = parameter.Split('=');
				if (pair.Length == 0)
					continue;

				switch (pair.Length)
				{
					case 1:
						parameters.Add(pair[0], string.Empty);
						break;
					case 2:
						parameters.Add(pair[0], pair[1]);
						break;
					default:
						parameters.Add(pair[0], string.Join("=", pair.Skip(1).Select(p => p)));
						break;
				}
			}

			return parameters;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="signedRequest"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		private static string GetValueFromSignedRequest(string signedRequest, string key)
		{
			if (string.IsNullOrEmpty(signedRequest))
				return null;

			if (string.IsNullOrEmpty(key))
				return null;

			// appData=&issuedAt=2014-01-01T00%3A00%3A00.000%2B0000&locale=en-US&networkEID=Fake_networkE1d&userEID=Fake_userE1d
			var data = signedRequest.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(p => !string.IsNullOrEmpty(p) && p.StartsWith(key));

			if (string.IsNullOrEmpty(data))
				return null;

			var value = (data.IndexOf('=') < 0 || data.IndexOf('=') == data.Length) ?
			                                                                        	string.Empty :
			                                                                        	             	data.Substring(data.IndexOf('=') + 1);

			return value == null ? null : Uri.UnescapeDataString(value);
		}

	}
}
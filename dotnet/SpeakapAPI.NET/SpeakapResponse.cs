using System.Net;

namespace SpeakapAPI
{
	/// <summary>
	/// 
	/// </summary>
	public class SpeakapResponse
	{
		/// <summary>
		/// 
		/// </summary>
		public HttpStatusCode Status { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public long Code { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public string Message { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public string Description { get; set; }
	}
}
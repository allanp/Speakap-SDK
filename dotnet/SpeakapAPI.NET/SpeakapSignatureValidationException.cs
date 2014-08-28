using System;

namespace SpeakapAPI
{
	/// <summary>
	/// Exception thrown when a signed request is invalid.
	/// </summary>
	public class SpeakapSignatureValidationException : Exception
	{
		public SpeakapSignatureValidationException(string message)
			: base(message)
		{
		}

		public override string ToString()
		{
			return Message;
		}
	}
}
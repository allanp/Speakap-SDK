using System;

namespace SpeakapAPI
{
	/// <summary>
	/// Exception thrown when a signed request is invalid.
	/// </summary>
	public class SignatureValidationErrorException : Exception
	{
		public SignatureValidationErrorException(string message)
			: base(message)
		{
		}

		public override string ToString()
		{
			return Message;
		}
	}
}
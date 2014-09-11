using System;

namespace Speakap.SDK
{
    public class SpeakapApplicationException : Exception
    {
        public SpeakapApplicationException(int code, string message) : base(message)
        {
            Code = code;
        }

        public int Code
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("{{ \"code\":{0},\"message\":\"{1}\"", Code, Message);
        }
    }
}

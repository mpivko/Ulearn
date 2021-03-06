using System;
using System.Runtime.Serialization;

namespace RunCheckerJob
{
	public class SandboxException : Exception
	{
		public SandboxException()
		{
		}

		public SandboxException(string message)
			: base(message)
		{
		}

		public SandboxException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected SandboxException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
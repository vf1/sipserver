using System;
using Mras.XmlContent;

namespace Mras
{
	public class MrasException
		: Exception
	{
		public ReasonPhrase ReasonPhrase { get; private set; }

		public MrasException()
			: base(Common.ToString(ReasonPhrase.RequestMalformed))
		{
			ReasonPhrase = ReasonPhrase.RequestMalformed;
		}

		public MrasException(ReasonPhrase reasonPhrase)
			: base(Common.ToString(reasonPhrase))
		{
			ReasonPhrase = reasonPhrase;
		}

		public MrasException(string message)
			: base(message)
		{
			ReasonPhrase = ReasonPhrase.RequestMalformed;
		}

		public MrasException(ReasonPhrase reasonPhrase, Exception innerException)
			: base(Common.ToString(reasonPhrase), innerException)
		{
			ReasonPhrase = reasonPhrase;
		}

		public MrasException(Exception innerException)
			: base(Common.ToString(ReasonPhrase.RequestMalformed), innerException)
		{
			ReasonPhrase = ReasonPhrase.RequestMalformed;
		}

		public MrasException(string message, Exception innerException)
			: base(message, innerException)
		{
			ReasonPhrase = ReasonPhrase.RequestMalformed;
		}
	}
}

using System;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	class GeneralVerifier
	{
		public enum Errors
		{
			None,
			MandatoryHeaderAbsent,
			HeaderDuplication,
			SipVersionNotSupported,
			ToTagAbsent,
		}

		public struct Result
		{
			private static readonly ByteArrayPart[] errorTexts;

			static Result()
			{
				errorTexts = new ByteArrayPart[]
				{
					new ByteArrayPart("No errors"),
					new ByteArrayPart("Mandatory header is absent or invalid"),
					new ByteArrayPart("Header duplication"),
					new ByteArrayPart("SIP version not supported"),
					new ByteArrayPart("To tag is absent"),
				};

				var values = Enum.GetValues(typeof(Errors));
				if (errorTexts.Length != values.Length)
					throw new InvalidProgramException(@"GeneralVerifier::Result static constructor");
			}

			public Errors Error;
			public HeaderNames HeaderName;

			public ByteArrayPart Message
			{
				get
				{
					return errorTexts[(int)Error];
				}
			}
		}

		public Result Validate(IncomingMessage message)
		{
			if (message.Reader.SipVersion != 20)
				return new Result() { Error = Errors.SipVersionNotSupported, };

			var absent = message.Reader.ValidateMandatoryHeaders();
			if (absent != HeaderNames.None)
				return new Result() { Error = Errors.MandatoryHeaderAbsent, HeaderName = absent, };

			// callcentric does not send to tag in the NOTIFY
			//if (message.Reader.From.Tag.IsNotEmpty == false)
			//	return new Result() { Error = Errors.ToTagAbsent, };

			//var duplicate = message.Reader.ValidateHeadersDuplication();
			//if (duplicate != HeaderNames.None)
			//    return new Result() { Error = Errors.HeaderDuplication, HeaderName = duplicate, };

			return new Result();
		}
	}
}

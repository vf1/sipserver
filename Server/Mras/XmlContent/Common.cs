using System;

namespace Mras.XmlContent
{
	public struct Version
	{
		public int Major { get; set; }
		public int Minor { get; set; }

		public static implicit operator string(Version version)
		{
			return version.Major.ToString() + "." + version.Minor.ToString();
		}

		public static implicit operator Version(string stringVersion)
		{
			Version version = new Version() { Major = 0, Minor = 0, };

			try
			{
				int point = stringVersion.IndexOf('.');

				version.Major = int.Parse(stringVersion.Substring(0, point));
				version.Minor = int.Parse(stringVersion.Substring(point + 1));
			}
			catch(Exception ex)
			{
				throw new MrasException(string.Format(@"{0} is invalid version format", stringVersion), ex);
			}

			return version;
		}

		public readonly static Version V1 = new Version() { Major = 1, Minor = 0 };
		public readonly static Version V2 = new Version() { Major = 2, Minor = 0 };
	}

	public enum Location
	{
		Intranet,
		Internet,
	}

	/// <summary>
	/// </summary>
	/// Update ToString too
	public enum Route
	{
		LoadBalanced,
		DirectIp,
	}

	/// <summary>
	/// </summary>
	/// Update ToString too
	public enum ReasonPhrase
	{
		OK,
		RequestMalformed,
		RequestTooLarge,
		NotSupported,
		ServerBusy,
		TimeOut,
		Forbidden,
		InternalServerError,
		OtherFailure,
		VersionMismatch,
	}

	public static class Common
	{
		public static Location ParseLocation(string location)
		{
			switch (location)
			{
				case @"intranet":
					return Location.Intranet;
				case @"internet":
					return Location.Internet;
			}

			throw new MrasException(string.Format(@"{0} is invalid location", location));
		}

		public static Route ParseRoute(string route)
		{
			switch (route)
			{
				case null:
				case @"loadbalanced":
					return Route.LoadBalanced;
				case @"directip":
					return Route.DirectIp;
			}

			throw new MrasException(string.Format(@"{0} is invalid route", route));
		}

		public static string ToString(this Location location)
		{
			switch (location)
			{
				case Location.Internet:
					return "internet";
				case Location.Intranet:
					return "intranet";
			}

			return string.Empty;
		}

		public static string ToString(this ReasonPhrase reasonPhrase)
		{
			switch(reasonPhrase)
			{
				case ReasonPhrase.OK:
					return "OK";
				case ReasonPhrase.RequestMalformed:
					return "Request Malformed";
				case ReasonPhrase.RequestTooLarge:
					return "Request Too Large";
				case ReasonPhrase.NotSupported:
					return "Not Supported";
				case ReasonPhrase.ServerBusy:
					return "Server Busy";
				case ReasonPhrase.TimeOut:
					return "Time Out";
				case ReasonPhrase.Forbidden:
					return "Forbidden";
				case ReasonPhrase.InternalServerError:
					return "Internal Server Error";
				case ReasonPhrase.OtherFailure:
					return "Other Failure";
				case ReasonPhrase.VersionMismatch:
					return "Version Mismatch";
			}

			return string.Empty;
		}
	}
}

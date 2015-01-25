using System;
using Base.Message;

namespace Server.Authorization
{
	interface IAuthorizationManager<R, W>
	{
		bool IsAuthorized(R reader, ArraySegment<byte> content, out W writer);
		bool IsAuthorized(R reader, ArraySegment<byte> content, ByteArrayPart realm, int param, out W writer);
	}

	enum AuthenticationKind
	{
		User,
		Proxy,
	}

	enum AuthorizationError
	{
		None,
		Failed,
		Continue,
		Success,
	}

	enum AuthorizationCommands
	{
		None,
		Cancel,
		Continue,
		TryAgain,
	}

	struct AuthorizationStatistic
	{
		public int NoneCount;
		public int FailedCount;
		public int ContinueCount;
		public int SuccessCount;

		public void Update(AuthorizationError error)
		{
			switch (error)
			{
				case AuthorizationError.None:
					NoneCount++;
					break;
				case AuthorizationError.Failed:
					FailedCount++;
					break;
				case AuthorizationError.Continue:
					ContinueCount++;
					break;
				case AuthorizationError.Success:
					SuccessCount++;
					break;
			}
		}
	}

	struct AuthorizationShedulerState<R>
	{
		public int Agent;
		public AuthorizationStatistic Statistic;
		public readonly R Reader;
		public readonly ArraySegment<byte> Content;
		public readonly ByteArrayPart Realm;
		public readonly int Param;

		public AuthorizationShedulerState(R reader, ArraySegment<byte> content, ByteArrayPart realm, int param)
		{
			Agent = 0;
			Statistic = new AuthorizationStatistic();

			Reader = reader;
			Content = content;
			Realm = realm;
			Param = param;
		}
	}

	struct AuthorizationShedulerResponse<W>
	{
		public readonly AuthorizationCommands Command;
		public readonly AuthorizationStatistic Statistic;
		public readonly W Writer;

		public AuthorizationShedulerResponse(AuthorizationCommands command, AuthorizationStatistic statistic, W writer)
		{
			Command = command;
			Statistic = statistic;
			Writer = writer;
		}
	}

	interface IAuthorizationSheduler<R, W, S>
	{
		bool ValidateAuthorization(R reader, ByteArrayPart username, int param);
		AuthorizationShedulerResponse<W> GetCommand(AuthorizationShedulerState<R> state, S scheme, AuthorizationError error);
	}

	struct AuthorizationAgentsState<W>
	{
		public readonly AuthorizationStatistic Statistic;
		public readonly W Writer;

		public AuthorizationAgentsState(AuthorizationShedulerResponse<W> response)
		{
			Statistic = response.Statistic;
			Writer = response.Writer;
		}
	}

	interface IAuthorizationAgent<R, W, S>
	{
		AuthorizationAgentsState<W> IsAuthorized(IAuthorizationSheduler<R, W, S> sheduler, AuthorizationShedulerState<R> state);
	}
}

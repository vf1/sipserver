using System;
using System.Collections.Generic;
using Base.Message;
using Sip.Message;

namespace Server.Authorization
{
	abstract class AuthorizationManager<R, W, S>
		: IAuthorizationManager<R, W>
		, IAuthorizationSheduler<R, W, S>
	{
		private List<IAuthorizationAgent<R, W, S>> agents;

		public AuthorizationManager()
		{
			this.IsEnabled = true;
			this.agents = new List<IAuthorizationAgent<R, W, S>>();
		}

		public bool IsAuthorized(R reader, ArraySegment<byte> content, out W writer)
		{
			return IsAuthorized(reader, content, ByteArrayPart.Invalid, 0, out writer);
		}

		public bool IsAuthorized(R reader, ArraySegment<byte> content, ByteArrayPart realm, int param, out W writer)
		{
			if (agents.Count <= 0 || IsEnabled == false)
			{
				writer = default(W);
				return true;
			}
			else
			{
				var response = agents[0].IsAuthorized(
					this,
					new AuthorizationShedulerState<R>(reader, content, realm, param));

				writer = response.Writer;

				if (writer != null)
					WriteMessageEnd(writer);

				return response.Statistic.SuccessCount > 0;
			}
		}

		public bool IsEnabled
		{
			get;
			set;
		}

		public void RegisterAgent(IAuthorizationAgent<R, W, S> agent, S scheme)
		{
			agents.Add(agent);
		}

		AuthorizationShedulerResponse<W> IAuthorizationSheduler<R, W, S>.GetCommand(AuthorizationShedulerState<R> state, S scheme, AuthorizationError error)
		{
			state.Statistic.Update(error);

			if (state.Agent < agents.Count - 1)
			{
				state.Agent++;

				var response = agents[state.Agent].IsAuthorized(this, state);

				var command = GetCommand(response.Statistic, scheme, error);
				var writer = GetWriter(command, state.Reader, response.Writer);
				return new AuthorizationShedulerResponse<W>(command, response.Statistic, writer);
			}
			else
			{
				var command = GetCommand(state.Statistic, scheme, error);
				var writer = GetWriter(command, state.Reader, default(W));
				return new AuthorizationShedulerResponse<W>(command, state.Statistic, writer);
			}
		}

		bool IAuthorizationSheduler<R, W, S>.ValidateAuthorization(R reader, ByteArrayPart username, int param)
		{
			return ValidateAuthorizationInternal(reader, username, param);
		}

		private W GetWriter(AuthorizationCommands command, R reader, W writer)
		{
			if (command == AuthorizationCommands.Continue || command == AuthorizationCommands.TryAgain || command == AuthorizationCommands.Cancel)
			{
				if (writer == null)
				{
					writer = GetResponseBegin(reader);
				}
			}

			return writer;
		}

		protected abstract bool ValidateAuthorizationInternal(R reader, ByteArrayPart username, int param);
		protected abstract W GetResponseBegin(R reader);
		protected abstract void WriteMessageEnd(W writer);
		protected abstract bool CanTryAgainWhenFail(S scheme);

		private AuthorizationCommands GetCommand(AuthorizationStatistic statistic, S scheme, AuthorizationError error)
		{
			switch (error)
			{
				case AuthorizationError.Success:
					return AuthorizationCommands.None;

				case AuthorizationError.Continue:
					return (statistic.SuccessCount > 0)
						? AuthorizationCommands.Cancel
						: AuthorizationCommands.Continue;

				case AuthorizationError.Failed:
					if (CanTryAgainWhenFail(scheme))
						if (statistic.SuccessCount == 0 && statistic.ContinueCount == 0)
							return AuthorizationCommands.TryAgain;
					return AuthorizationCommands.Cancel;

				case AuthorizationError.None:
					return (statistic.SuccessCount > 0 || statistic.ContinueCount > 0 || statistic.FailedCount > 0)
						? AuthorizationCommands.None
						: AuthorizationCommands.TryAgain;

				default:
					throw new InvalidProgramException();
			}
		}
	}
}


using System;
using System.Collections;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	partial class LocationService
	{
		sealed class Bindings
			: IEnumerable<Binding>
		{
			private readonly object sync;
			private readonly byte[] aor;
			private Binding[] bindings;
			private bool isNew;
			private bool isStale;

			[ThreadStatic]
			private static SkipNullArrayEnumerator<Binding> enumerator;

			public Bindings(ByteArrayPart toAor)
			{
				isStale = false;
				isNew = true;

				aor = new byte[toAor.Length];
				toAor.BlockCopyTo(aor, 0);

				bindings = new Binding[1];

				sync = this.aor;
			}

			public bool IsStale
			{
				get { return isStale; }
			}

			public bool IsNew
			{
				get { return isNew; }
			}

			public ByteArrayPart Aor
			{
				get { return new ByteArrayPart() { Bytes = aor, Begin = 0, End = aor.Length, }; }
			}

			public bool TryRemoveExpired(Action<Bindings, Binding> onRemoveBinding)
			{
				lock (sync)
				{
					if (isStale)
						return false;

					for (int i = 0; i < bindings.Length; i++)
						if (bindings[i] != null && bindings[i].IsExpired)
							RemoveBinding(i, onRemoveBinding);

					UpdateStale();
					return true;
				}
			}

			public bool TryRemoveAll(Action<Bindings, Binding> onRemoveBinding)
			{
				lock (sync)
				{
					if (isStale)
						return false;

					for (int i = 0; i < bindings.Length; i++)
						if (bindings[i] != null && bindings[i].IsExpired)
							RemoveBinding(i, onRemoveBinding);

					UpdateStale();
					return true;
				}
			}

			public bool TryRemoveByConnectionId(int connectionId, Action<Bindings, Binding> onRemoveBinding)
			{
				lock (sync)
				{
					if (isStale)
						return false;

					for (int i = 0; i < bindings.Length; i++)
						if (bindings[i] != null && bindings[i].ConnectionAddresses.ConnectionId == connectionId)
							RemoveBinding(i, onRemoveBinding);

					UpdateStale();
					return true;
				}
			}

			public bool TryUpdate(IncomingMessageEx request, int defaultExpires, out bool isNewData, Action<Bindings, Binding, SipMessageReader> onAddBinding, Action<Bindings, Binding> onRemoveBinding)
			{
				isNewData = false;

				lock (sync)
				{
					if (isStale)
						return false;

					isNewData = IsNewData(request.Reader.CallId, request.Reader.CSeq.Value);

					if (isNewData)
					{
						var reader = request.Reader;

						for (int i = 0; i < reader.Count.ContactCount; i++)
						{
							int index = FindBindingByAddrSpec(reader.Contact[i].AddrSpec.Value);
							int expires = GetExpires(reader, i, defaultExpires);

							if (expires > 0)
							{
								if (index >= 0 && bindings[index].IsChanged(reader, i) == false)
								{
									bindings[index].Update(reader.CSeq.Value, expires);
								}
								else
								{
									if (index < 0 && reader.Contact[i].SipInstance.IsValid)
										index = FindBindingBySipInstance(reader.Contact[i].SipInstance);

									if (index >= 0)
										RemoveBinding(index, onRemoveBinding);

									AddBinding(new Binding(reader, i, expires, request.ConnectionAddresses), onAddBinding, reader);
								}
							}
							else
							{
								if (index >= 0)
									RemoveBinding(index, onRemoveBinding);
							}
						}
					}

					UpdateStale();
					return true;
				}
			}

			private bool IsNewData(ByteArrayPart callId, int cseq)
			{
				foreach (var binging in bindings)
					if (binging != null && binging.IsNewData(callId, cseq) == false)
						return false;

				return true;
			}

			private void UpdateStale()
			{
				for (int i = 0; i < bindings.Length; i++)
					if (bindings[i] != null)
						return;

				isStale = true;
			}

			private static int GetExpires(SipMessageReader reader, int contactIndex, int defaultExpires)
			{
				int expires;

				if (reader.Contact[contactIndex].Expires != int.MinValue)
					expires = reader.Contact[contactIndex].Expires;
				else if (reader.Expires != int.MinValue)
					expires = reader.Expires;
				else
					expires = defaultExpires;

				return expires;
			}

			#region IEnumerable<Binding>

			public IEnumerator<Binding> GetEnumerator()
			{
				if (enumerator == null)
					enumerator = new SkipNullArrayEnumerator<Binding>(16);

				lock (sync)
					enumerator.Initialize(bindings);

				return enumerator;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion

			#region bindings[] Helpers

			private int FindBindingByAddrSpec(ByteArrayPart addrSpec)
			{
				for (int i = 0; i < bindings.Length; i++)
					if (bindings[i] != null && bindings[i].AddrSpec == addrSpec)
						return i;

				return -1;
			}

			private int FindBindingBySipInstance(ByteArrayPart sipInstance)
			{
				if (bindings != null)
					for (int i = 0; i < bindings.Length; i++)
						if (bindings[i] != null && bindings[i].SipInstance == sipInstance)
							return i;

				return -1;
			}

			private int AddBinding()
			{
				for (int i = 0; i < bindings.Length; i++)
					if (bindings[i] == null)
						return i;

				int length = bindings.Length;

				Array.Resize<Binding>(ref bindings, length + 1);

				return length;
			}

			private Binding FindBinding(ByteArrayPart addrSpec)
			{
				int index = FindBindingByAddrSpec(addrSpec);

				return (index < 0) ? null : bindings[index];
			}

			private void RemoveBinding(int index, Action<Bindings, Binding> onRemoveBinding)
			{
				var binding = bindings[index];
				bindings[index] = null;

				onRemoveBinding(this, binding);
			}

			private void AddBinding(Binding binding, Action<Bindings, Binding, SipMessageReader> onAddBinding, SipMessageReader param)
			{
				int index = AddBinding();
				bindings[index] = binding;

				onAddBinding(this, binding, param);

				isNew = false;
			}

			#endregion
		}
	}
}

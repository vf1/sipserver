using System;
using System.Collections;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using System.Threading;
using System.Linq;
using Sip.Message;
using Base.Message;
using Sip.Tools;

namespace Sip.Server
{
	partial class LocationService
		: IDisposable
		, ILocationService
	{
		private readonly object sync;
		private readonly byte[] scheme;
		private readonly MultiTimer<ByteArrayPart> timer;
		private readonly Dictionary<ByteArrayPart, Bindings> bindingsByAor;
		private readonly ThreadSafe.Dictionary<int, ByteArrayPart> bindingsByConnectionId;

		private readonly Action<Bindings, Binding, SipMessageReader> ContactAddedHandler;
		private readonly Action<Bindings, Binding> ContactRemovedHandler;

		public event Action<ByteArrayPart> AorAdded;
		public event Action<ByteArrayPart> AorRemoved;
		public event Action<ByteArrayPart, Binding, SipMessageReader> ContactAdded;
		public event Action<ByteArrayPart, Binding> ContactRemoved;

		public LocationService()
		{
			this.ContactAddedHandler = ContactAddedHandlerInternal;
			this.ContactRemovedHandler = ContactRemovedHandlerInternal;

			this.scheme = System.Text.Encoding.UTF8.GetBytes(@"sip");
			this.timer = new MultiTimer<ByteArrayPart>(RemoveExpiredBindings, 16384,
				false, ByteArrayPartEqualityComparer.GetStaticInstance());
			this.bindingsByAor = new Dictionary<ByteArrayPart, Bindings>();
			this.bindingsByConnectionId = new ThreadSafe.Dictionary<int, ByteArrayPart>(16384);
			this.sync = timer;
		}

		public void Dispose()
		{
			timer.Dispose();
		}

		public IEnumerable<Binding> GetEnumerableBindings(ByteArrayPart user, ByteArrayPart domain)
		{
			return GetEnumerableBindings(GetAor(user, domain));
		}

		public IEnumerable<Binding> GetEnumerableBindings(ByteArrayPart aor)
		{
			return GetBindings(aor) ?? EmptyEnumerable<Binding>.Instance;
		}

		public bool UpdateBindings(ByteArrayPart user, ByteArrayPart domain, IncomingMessageEx request, int defaultExpires)
		{
			bool isNewData;
			Bindings bindings;

			do
			{
				bindings = GetOrAddBindings(GetAor(user, domain));

			} while (bindings.TryUpdate(request, defaultExpires, out isNewData, ContactAddedHandler, ContactRemovedHandler) == false);

			RemoveStaleBindings(bindings);

			return isNewData;
		}

		public void RemoveAllBindings(ByteArrayPart user, ByteArrayPart domain)
		{
			Bindings bindings = GetBindings(GetAor(user, domain));

			if (bindings != null)
			{
				if (bindings.TryRemoveAll(ContactRemovedHandler))
					RemoveStaleBindings(bindings);
			}
		}

		public void RemoveBindingsWhenConnectionEnd(int connectionId)
		{
			ByteArrayPart aor;
			if (bindingsByConnectionId.TryGetValue(connectionId, out aor))
			{
				Bindings bindings = GetBindings(aor);

				if (bindings != null)
				{
					if (bindings.TryRemoveByConnectionId(connectionId, ContactRemovedHandler))
						RemoveStaleBindings(bindings);
				}
			}
		}

		private void RemoveExpiredBindings(ByteArrayPart aor)
		{
			Bindings bindings = GetBindings(aor);

			if (bindings != null)
			{
				if (bindings.TryRemoveExpired(ContactRemovedHandler))
					RemoveStaleBindings(bindings);
			}
		}

		private void ContactAddedHandlerInternal(Bindings bindings, Binding binding, SipMessageReader request)
		{
			timer.Add(binding.Expires * 1000, bindings.Aor);

			if (binding.ConnectionAddresses.Transport == Transports.Tcp || binding.ConnectionAddresses.Transport == Transports.Tls)
				bindingsByConnectionId.Replace(binding.ConnectionAddresses.ConnectionId, bindings.Aor);

			if (bindings.IsNew)
				AorAdded(bindings.Aor);

			ContactAdded(bindings.Aor, binding, request);
		}

		private void ContactRemovedHandlerInternal(Bindings bindings, Binding binding)
		{
			if (binding.ConnectionAddresses.Transport == Transports.Tcp || binding.ConnectionAddresses.Transport == Transports.Tls)
				bindingsByConnectionId.Remove(binding.ConnectionAddresses.ConnectionId);

			ContactRemoved(bindings.Aor, binding);

			if (bindings.IsStale)
				AorRemoved(bindings.Aor);
		}

		#region bindingsByAor Helpers

		private Bindings GetOrAddBindings(ByteArrayPart aor)
		{
			Bindings bindings;

			lock (sync)
			{
				if (bindingsByAor.TryGetValue(aor, out bindings))
				{
					if (bindings.IsStale)
					{
						bindingsByAor.Remove(bindings.Aor);
						bindings = null;
					}
				}

				if (bindings == null)
				{
					bindings = new Bindings(aor);
					bindingsByAor.Add(bindings.Aor, bindings);
				}
			}

			return bindings;
		}

		private Bindings GetBindings(ByteArrayPart aor)
		{
			Bindings bindings;

			lock (sync)
			{
				if (bindingsByAor.TryGetValue(aor, out bindings))
				{
					if (bindings.IsStale)
					{
						bindingsByAor.Remove(bindings.Aor);
						bindings = null;
					}
				}
			}

			return bindings;
		}

		private void RemoveStaleBindings(Bindings bindings)
		{
			if (bindings != null && bindings.IsStale)
			{
				lock (sync)
				{
					if (bindings.IsStale)
						bindingsByAor.Remove(bindings.Aor);
				}
			}
		}

		[ThreadStatic]
		private static byte[] tempAor;

		private ByteArrayPart GetAor(ByteArrayPart user, ByteArrayPart domainName)
		{
			if (user.IsInvalid || domainName.IsInvalid)
				return ByteArrayPart.Invalid;

			int length = scheme.Length + 1 + user.Length + 1 + domainName.Length;
			if (tempAor == null || tempAor.Length < length)
				tempAor = new byte[Math.Max(length, 256)];

			Buffer.BlockCopy(scheme, 0, tempAor, 0, scheme.Length);
			Buffer.BlockCopy(user.Bytes, user.Offset, tempAor, scheme.Length + 1, user.Length);
			Buffer.BlockCopy(domainName.Bytes, domainName.Offset, tempAor, scheme.Length + 1 + user.Length + 1, domainName.Length);

			tempAor[scheme.Length] = 0x3a;
			tempAor[scheme.Length + 1 + user.Length] = 0x40;

			return new ByteArrayPart() { Bytes = tempAor, Begin = 0, End = length };
		}

		#endregion
	}
}

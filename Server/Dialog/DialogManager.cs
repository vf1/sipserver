using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using ThreadSafe = System.Collections.Generic.ThreadSafe;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
	public class DialogManager
	{
		private static int localTagCount;
		[ThreadStatic]
		private static byte[] dialogIdBytes;
		private ThreadSafe.Dictionary<ByteArrayPart, Dialog> dictionary;

		public DialogManager()
		{
			dictionary = new ThreadSafe.Dictionary<ByteArrayPart, Dialog>(
				new Dictionary<ByteArrayPart, Dialog>(ByteArrayPartEqualityComparer.GetStaticInstance()));
		}

		public Dialog Get(SipMessageReader message)
		{
			if (Dialog.HasValidId(message))
				return dictionary.TryGetValue(GetDialogId(message));
			return null;
		}

		public Dialog Get(ByteArrayPart dialogId)
		{
			return dictionary.TryGetValue(dialogId);
		}

		public Dialog Create(SipMessageReader message, int localTag, ConnectionAddresses connectionAddresses)
		{
			var dialog = new Dialog(message, localTag, connectionAddresses);

			dictionary.Add(dialog.Id, dialog);

			return dialog;
		}

		/// <summary>
		/// DEPRECATED!!! Не удобно читать код, не видно откуда CallLegTransactionDoesNotExist
		/// </summary>
		public Dialog GetOrCreate(SipMessageReader reader, ConnectionAddresses connectionAddresses, out StatusCodes statusCode)
		{
			var dialog = GetOrCreate(reader, connectionAddresses);

			statusCode = (dialog == null) ?
				StatusCodes.CallLegTransactionDoesNotExist : StatusCodes.OK;

			return dialog;
		}

		public Dialog GetOrCreate(SipMessageReader reader, ConnectionAddresses connectionAddresses)
		{
			Dialog dialog = null;

			if (reader.To.Tag.IsInvalid)
				dialog = Create(reader, NewLocalTag(), connectionAddresses);
			else
				dialog = Get(reader);

			return dialog;
		}

		public void Remove(ByteArrayPart dialogId)
		{
			dictionary.Remove(dialogId);
		}

		public static int NewLocalTag()
		{
			return Interlocked.Increment(ref localTagCount);
		}

		private static ByteArrayPart GetDialogId(SipMessageReader message)
		{
			if (Dialog.HasValidId(message) == false)
				return ByteArrayPart.Invalid;

			int length = Dialog.GetIdLength(message);

			if (dialogIdBytes == null || dialogIdBytes.Length < length)
				dialogIdBytes = new byte[length];

			var part = new ByteArrayPart()
			{
				Bytes = dialogIdBytes,
				Begin = 0,
				End = length,
			};

			Dialog.GenerateId(message, part.Bytes);

			return part;
		}
	}
}

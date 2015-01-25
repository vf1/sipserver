using System;
using System.Text;
using Sip.Message;
using Sip.Tools;

namespace Sip.Server
{
	struct BadServerTransactionKey
		: IEquatable<BadServerTransactionKey>
	{
		private readonly byte[] bytes;
		private int? transactionId;
		private int hashCode;

		public BadServerTransactionKey(int transactionId, int hashCode)
		{
			this.bytes = null;
			this.hashCode = hashCode;
			this.transactionId = transactionId;
		}

		public BadServerTransactionKey(SipMessageReader reader, Methods method)
		{
			this.transactionId = null;

			int offset;

			if (reader.Via[0].Branch.StartsWith(SipMessage.MagicCookie))
			{
				int count =
					reader.Via[0].Branch.Length + 1 +
					reader.Via[0].SentBy.Host.Length +
					16;

				bytes = new byte[count];

				offset = bytes.CopyFrom(reader.Via[0].Branch, 0) + 1;
				offset = bytes.CopyFrom(reader.Via[0].SentBy.Host, offset);
			}
			else
			{
				bool useToTag = reader.To.Tag.IsValid && method != Methods.Invitem;

				int count =
					reader.RequestUri.Value.Length + 1 +
					reader.From.Tag.Length + 1 +
					reader.CallId.Length + 1 +
					reader.Via[0].SentBy.Host.Length + 1 +
					(useToTag ? reader.To.Tag.Length : 0) +
					16;

				bytes = new byte[count];

				offset = bytes.CopyFrom(reader.RequestUri.Value, 0) + 1;
				offset = bytes.CopyFrom(reader.From.Tag, offset) + 1;
				offset = bytes.CopyFrom(reader.CallId, offset) + 1;
				offset = bytes.CopyFrom(reader.Via[0].SentBy.Host, offset) + 1;
				if (useToTag)
					offset = bytes.CopyFrom(reader.To.Tag, offset);
			}

			offset = bytes.CopyFrom((int)method, offset);
			offset = bytes.CopyFrom((int)reader.Via[0].Transport, offset);
			offset = bytes.CopyFrom(reader.Via[0].SentBy.Port, offset);
			offset = bytes.CopyFrom(reader.CSeq.Value, offset);

			hashCode = bytes.GetValueHashCode();
		}

		public int TransactionId
		{
			get { return transactionId.HasValue ? transactionId.Value : -1; }
			set { transactionId = value; }
		}

		public bool Equals(BadServerTransactionKey y)
		{
			if (transactionId != null && y.transactionId != null)
				return transactionId == y.transactionId;

			return bytes.IsEqualValue(y.bytes);
		}

		public override int GetHashCode()
		{
			return hashCode;
		}
	}
}

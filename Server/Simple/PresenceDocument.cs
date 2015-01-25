using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Http.Message;
using Sip.Message;
using Base.Message;

namespace Sip.Simple
{
	public sealed class PresenceDocument
	{
		private readonly string aor;
		private readonly List<Tuple> tuples;
		private int modifiedCount;

		private static readonly XmlReaderSettings readerSettings = new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true, };
		private static readonly XmlWriterSettings writerSettings = new XmlWriterSettings() { Indent = false, OmitXmlDeclaration = true, };
		private static readonly byte[] documentHeader = Encoding.UTF8.GetBytes(@"<?xml version='1.0' encoding='UTF-8'?><presence xmlns='urn:ietf:params:xml:ns:pidf'>");
		private static readonly byte[] documentFooter = Encoding.UTF8.GetBytes(@"</presence>");
		private static readonly byte[] defaultTuple0 = Encoding.UTF8.GetBytes("<tuple id=\"sipserver0123\"><status><basic>close</basic></status><contact>sip:");
		private static readonly byte[] defaultTuple1 = Encoding.UTF8.GetBytes(@"</contact><note>Offline</note></tuple>");

		#region struct Tuple {...}

		struct Tuple
		{
			public string Id;
			public string LocalName;
			public int SipEtag;
			public ArraySegment<byte> Data;
		}

		#endregion

		public PresenceDocument(string aor)
		{
			this.aor = aor;
			this.modifiedCount = 0;
			this.tuples = new List<Tuple>();
		}

		internal bool Modify(int sipIfMatch, ArraySegment<byte> pidf)
		{
			var newTuples = GetTuples(pidf, sipIfMatch);

			bool isPidfValid = newTuples != null;

			if (isPidfValid)
			{
				int tupleIndex = 0;

				lock (tuples)
				{
					for (int i = tuples.Count - 1; i >= 0; i--)
					{
						if (tuples[i].SipEtag == sipIfMatch)
							tuples.RemoveAt(i);
						else
						{
							for (int j = 0; j < newTuples.Count; j++)
								if (newTuples[j].Id == tuples[i].Id)
								{
									tuples.RemoveAt(i);
									break;
								}
						}
					}

					for (int i = 0; i < newTuples.Count; i++)
					{
						var tuple = newTuples[i];

						if (tuple.LocalName == @"tuple")
							tuples.Insert(tupleIndex++, tuple);
						else
							tuples.Add(tuple);
					}
				}

				Interlocked.Increment(ref modifiedCount);
			}

			return isPidfValid;
		}

		internal void Remove(int sipIfMatch)
		{
			bool removed = false;

			lock (tuples)
			{
				for (int i = tuples.Count - 1; i >= 0; i--)
				{
					if (tuples[i].SipEtag == sipIfMatch)
					{
						tuples.RemoveAt(i);
						removed = true;
					}
				}
			}

			if (removed)
				Interlocked.Increment(ref modifiedCount);
		}

		internal bool ResetChangedCount()
		{
			return Interlocked.Exchange(ref modifiedCount, 0) > 0;
		}

		internal int[] GetAllEtags()
		{
			lock (tuples)
			{
				// may have dupes, it is possible to remove dupes for optimization
				var etags = new int[tuples.Count];
				for (int i = 0; i < tuples.Count; i++)
					etags[i] = tuples[i].SipEtag;

				return etags;
			}
		}

		//public int CopyTo(Func<int, ArraySegment<byte>> getBuffer)
		//{
		//    lock (tuples)
		//    {
		//        int length = CountLength();

		//        var segment = getBuffer(length);

		//        if (segment.Count >= length)
		//        {
		//            int offset = 0;
		//            BlockCopy(openPresence, segment, ref offset, openPresence.Length);

		//            for (int i = 0; i < tuples.Count; i++)
		//            {
		//                Buffer.BlockCopy(tuples[i].Data.Array, tuples[i].Data.Offset,
		//                    segment.Array, segment.Offset + offset, tuples[i].Data.Count);
		//                offset += tuples[i].Data.Count;
		//            }

		//            BlockCopy(closePresence, segment, ref offset, closePresence.Length);

		//            return length;
		//        }

		//        return 0;
		//    }
		//}

		public void WriteLenghtAndContent(HttpMessageWriter writer)
		{
			lock (tuples)
			{
				int length = CountLength();

				writer.WriteContentLength(length);
				writer.WriteCRLF();

				WriteContent(writer, length);
			}
		}

		public void WriteLenghtAndContent(SipMessageWriter writer)
		{
			lock (tuples)
			{
				int length = CountLength();

				writer.WriteContentLength(length);
				writer.WriteCRLF();

				WriteContent(writer, length);
			}
		}

		private void WriteContent(ByteArrayWriter writer, int length)
		{
			writer.ValidateCapacity(length);

			writer.Write(documentHeader);

			if (tuples.Count > 0)
			{
				for (int i = 0; i < tuples.Count; i++)
					writer.Write(tuples[i].Data);
			}
			else
			{
				writer.Write(defaultTuple0);
				writer.Write(aor);
				writer.Write(defaultTuple1);
			}

			writer.Write(documentFooter);
		}

		public void WriteContent(ByteArrayWriter writer)
		{
			lock (tuples)
				WriteContent(writer, CountLength());
		}

		private int CountLength()
		{
			int length = 0;

			if (tuples.Count > 0)
			{
				for (int i = 0; i < tuples.Count; i++)
					length += tuples[i].Data.Count;
			}
			else
			{
				length += defaultTuple0.Length;
				length += Encoding.UTF8.GetByteCount(aor);
				length += defaultTuple1.Length;
			}

			return documentHeader.Length + length + documentFooter.Length;
		}

		//private static void BlockCopy(byte[] src, ArraySegment<byte> dst, ref int offset, int length)
		//{
		//    Buffer.BlockCopy(src, 0, dst.Array, dst.Offset + offset, length);
		//    offset += length;
		//}

		private List<Tuple> GetTuples(ArraySegment<byte> pidf, int sipIfMatch)
		{
			var tuples = new List<Tuple>(16);

			try
			{
				using (var inStream = new MemoryStream(pidf.Array, pidf.Offset, pidf.Count))
				using (var reader = XmlReader.Create(inStream, readerSettings))
				{
					if (reader.ReadToFollowing(@"presence", @"urn:ietf:params:xml:ns:pidf") && reader.Read())
					{
						for (; ; )
						{
							while (reader.NodeType != XmlNodeType.Element || string.IsNullOrEmpty(reader.GetAttribute(@"id")))
							{
								if (reader.Read() == false)
									break;
							}

							if (reader.EOF)
								break;

							using (var outStream = new MemoryStream())
							using (var writer = XmlWriter.Create(outStream, writerSettings))
							{
								var tuple = new Tuple()
								{
									Id = reader.GetAttribute(@"id"),
									LocalName = reader.LocalName,
									SipEtag = sipIfMatch,
								};

								writer.WriteNode(reader.ReadSubtree(), true);
								writer.Flush();

								var buffer = outStream.GetBuffer();
								int offset = (buffer[0] != 60) ? 3 : 0;
								tuple.Data = new ArraySegment<byte>(buffer, offset, (int)outStream.Length - offset);
								tuples.Add(tuple);
							}
						}
					}
				}
			}
			catch (XmlException)
			{
				return null;
			}

			return tuples;
		}
	}
}

using System;
using Rtp;
using NUnit.Framework;

namespace TestRtp
{
	[TestFixture]
	public class DtmfDecoderTest
	{
		// NTPTimestamp: 0xCEA3F3344AE04C05
		// RTPTimestamp: 0x06E04447
		byte[] client_rtcp_0 = new byte[] { 0x81, 0xC8, 0x00, 0x0C, 0x00, 0x01, 0x50, 0x61, 0xCE, 0xA3, 0xF3, 0x34, 0x4A, 0xE0, 0x4C, 0x05, 0x06, 0xE0, 0x44, 0x47, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x08, 0x35, 0xEA, 0x70, 0x89, 0x6C, 0x13, 0x00, 0x00, 0x03, 0x00, 0x00, 0xBD, 0x47, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x81, 0xCA, 0x00, 0x06, 0x00, 0x01, 0x50, 0x61, 0x01, 0x11, 0x75, 0x73, 0x65, 0x72, 0x40, 0x67, 0x69, 0x7A, 0x6D, 0x6F, 0x70, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x00, };

		//20870	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18012, TimeStamp = 115840423, Mark
		//20891	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18013, TimeStamp = 115840423
		//20899	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18014, TimeStamp = 115840423
		//20913	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18015, TimeStamp = 115840423
		//20914	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18016, TimeStamp = 115840423
		//20915	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18017, TimeStamp = 115840423

		byte[] client_1_1 = new byte[] { 0x80, 0xEA, 0x46, 0x5C, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x0A, 0x03, 0xC0, };
		byte[] client_1_2 = new byte[] { 0x80, 0x6A, 0x46, 0x5D, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x0A, 0x07, 0x80, };
		byte[] client_1_3 = new byte[] { 0x80, 0x6A, 0x46, 0x5E, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x0A, 0x0B, 0x40, };
		byte[] client_1_4 = new byte[] { 0x80, 0x6A, 0x46, 0x5F, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x8A, 0x0F, 0x00, };
		byte[] client_1_5 = new byte[] { 0x80, 0x6A, 0x46, 0x60, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x8A, 0x0F, 0x00, };
		byte[] client_1_6 = new byte[] { 0x80, 0x6A, 0x46, 0x61, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x8A, 0x0F, 0x00, };

		//20991	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 995828382, Seq = 33112, TimeStamp = 1891476472, Mark
		//21005	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 995828382, Seq = 33113, TimeStamp = 1891476472
		//21028	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 995828382, Seq = 33114, TimeStamp = 1891476472
		//21040	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 129139462, Seq = 62582, TimeStamp = 0, Mark
		//21041	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 129139462, Seq = 62583, TimeStamp = 0
		//21042	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 129139462, Seq = 62584, TimeStamp = 0
		//21043	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 129139462, Seq = 62585, TimeStamp = 0
		//21044	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 129139462, Seq = 62585, TimeStamp = 0
		//21045	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 129139462, Seq = 62585, TimeStamp = 0
		//21056	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 995828382, Seq = 33115, TimeStamp = 1891476472
		//21069	120.136.44.17	192.168.1.15	RTP	RTP:PayloadType = dynamic, SSRC = 995828382, Seq = 33116, TimeStamp = 1891476472

		byte[] server_A_1_1 = new byte[] { 0x80, 0xEA, 0x81, 0x58, 0x70, 0xBD, 0xA3, 0xF8, 0x3B, 0x5B, 0x22, 0x9E, 0x01, 0x0A, 0x00, 0x00, };
		byte[] server_A_1_2 = new byte[] { 0x80, 0x6A, 0x81, 0x59, 0x70, 0xBD, 0xA3, 0xF8, 0x3B, 0x5B, 0x22, 0x9E, 0x01, 0x0A, 0x03, 0xC0, };
		byte[] server_A_1_3 = new byte[] { 0x80, 0x6A, 0x81, 0x5A, 0x70, 0xBD, 0xA3, 0xF8, 0x3B, 0x5B, 0x22, 0x9E, 0x01, 0x0A, 0x07, 0x80, };
		byte[] server_B_1_4 = new byte[] { 0x80, 0xEA, 0xF4, 0x76, 0x00, 0x00, 0x00, 0x00, 0x07, 0xB2, 0x83, 0x06, 0x01, 0x0A, 0x00, 0x00, };
		byte[] server_B_1_5 = new byte[] { 0x80, 0x6A, 0xF4, 0x77, 0x00, 0x00, 0x00, 0x00, 0x07, 0xB2, 0x83, 0x06, 0x01, 0x0A, 0x00, 0x00, };
		byte[] server_B_1_6 = new byte[] { 0x80, 0x6A, 0xF4, 0x78, 0x00, 0x00, 0x00, 0x00, 0x07, 0xB2, 0x83, 0x06, 0x01, 0x0A, 0x00, 0x00, };
		byte[] server_B_1_7 = new byte[] { 0x80, 0x6A, 0xF4, 0x79, 0x00, 0x00, 0x00, 0x00, 0x07, 0xB2, 0x83, 0x06, 0x01, 0x8A, 0x03, 0x20, };
		byte[] server_B_1_8 = new byte[] { 0x80, 0x6A, 0xF4, 0x79, 0x00, 0x00, 0x00, 0x00, 0x07, 0xB2, 0x83, 0x06, 0x01, 0x8A, 0x03, 0x20, };
		byte[] server_B_1_9 = new byte[] { 0x80, 0x6A, 0xF4, 0x79, 0x00, 0x00, 0x00, 0x00, 0x07, 0xB2, 0x83, 0x06, 0x01, 0x8A, 0x03, 0x20, };
		byte[] server_A_1_10 = new byte[] { 0x80, 0x6A, 0x81, 0x5B, 0x70, 0xBD, 0xA3, 0xF8, 0x3B, 0x5B, 0x22, 0x9E, 0x01, 0x0A, 0x0B, 0x40, };
		byte[] server_A_1_11 = new byte[] { 0x80, 0x6A, 0x81, 0x5C, 0x70, 0xBD, 0xA3, 0xF8, 0x3B, 0x5B, 0x22, 0x9E, 0x01, 0x8A, 0x0F, 0x00, };

		//21470	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18047, TimeStamp = 115873063, Mark
		//21483	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18048, TimeStamp = 115873063
		//21495	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18049, TimeStamp = 115873063
		//21513	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18050, TimeStamp = 115873063
		//21514	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18051, TimeStamp = 115873063
		//21515	192.168.1.15	120.136.44.17	RTP	RTP:PayloadType = dynamic, SSRC = 86113, Seq = 18052, TimeStamp = 115873063

		byte[] client_2_1 = new byte[] { 0x80, 0xEA, 0x46, 0x7F, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x0A, 0x03, 0xC0, };
		byte[] client_2_2 = new byte[] { 0x80, 0x6A, 0x46, 0x80, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x0A, 0x07, 0x80, };
		byte[] client_2_3 = new byte[] { 0x80, 0x6A, 0x46, 0x81, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x0A, 0x0B, 0x40, };
		byte[] client_2_4 = new byte[] { 0x80, 0x6A, 0x46, 0x82, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x8A, 0x0F, 0x00, };
		byte[] client_2_5 = new byte[] { 0x80, 0x6A, 0x46, 0x83, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x8A, 0x0F, 0x00, };
		byte[] client_2_6 = new byte[] { 0x80, 0x6A, 0x46, 0x84, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x8A, 0x0F, 0x00, };

		// SequenceNumber = client_1_1.SequenceNumber + 1
		byte[] client_1_1_x = new byte[] { 0x80, 0xEA, 0x46, 0x5D, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x0A, 0x03, 0xC0, };
		// SequenceNumber = client_1_1.SequenceNumber + 2
		byte[] client_1_1_y = new byte[] { 0x80, 0x6A, 0x46, 0x5E, 0x06, 0xE7, 0x95, 0xA7, 0x00, 0x01, 0x50, 0x61, 0x01, 0x0A, 0x03, 0xC0, };
		// SequenceNumber = client_1_1.SequenceNumber - 1
		byte[] client_2_1_x = new byte[] { 0x80, 0xEA, 0x46, 0x5B, 0x06, 0xE8, 0x15, 0x27, 0x00, 0x01, 0x50, 0x61, 0x02, 0x0A, 0x03, 0xC0, };

		[Test]
		public void DtmfDecoderTest1()
		{
			byte[][] pack1 = new byte[][] 
			{ 
				client_1_1, client_1_2, client_1_3, client_1_4, client_1_5, client_1_6, 
				client_2_1, client_2_2, client_2_3, client_2_4, client_2_5, client_2_6, 
			};
			var decoder1 = ProcessAll(pack1);
			var decoder2 = ProcessEach(pack1);

			Assert.AreEqual(2, decoder1.DtmfCodes.Count, "ProcessAll => DtmfCodes.Count");
			Assert.AreEqual((byte)1, decoder1.DtmfCodes[0].Code, "ProcessAll => DtmfCodes[0].Code");
			Assert.AreEqual((byte)2, decoder1.DtmfCodes[1].Code, "ProcessAll => DtmfCodes[1].Code");
			Assert.AreEqual(pack1.Length, decoder1.Proccessed);

			Assert.AreEqual(2, decoder2.DtmfCodes.Count, "ProcessEach => DtmfCodes.Count");
			Assert.AreEqual((byte)1, decoder2.DtmfCodes[0].Code, "ProcessEach => DtmfCodes[0].Code");
			Assert.AreEqual((byte)2, decoder2.DtmfCodes[1].Code, "ProcessEach => DtmfCodes[1].Code");
			Assert.AreEqual(pack1.Length, decoder2.Proccessed);
		}

		[Test]
		public void DtmfDecoderTest2()
		{
			byte[][] pack1 = new byte[][] { client_1_2, client_1_1, };
			var decoder = ProcessEach(pack1);

			Assert.AreEqual(1, decoder.DtmfCodes.Count);
			Assert.AreEqual((byte)1, decoder.DtmfCodes[0].Code);
			Assert.AreEqual(1, decoder.Proccessed);
		}

		[Test]
		public void DtmfDecoderTest3()
		{
			byte[][] pack1 = new byte[][] { client_1_1, client_1_1, };
			var decoder = ProcessAll(pack1);

			Assert.AreEqual(1, decoder.DtmfCodes.Count);
			Assert.AreEqual(1, decoder.Proccessed);
		}

		[Test]
		public void DtmfDecoderTest4()
		{
			// одинаковый Timestamp, client_1_1_y.Marker сброшен
			byte[][] pack1 = new byte[][] { client_1_1, client_1_1_x, client_1_1_y, };
			var decoder = ProcessAll(pack1);

			Assert.AreEqual(2, decoder.DtmfCodes.Count);
			Assert.AreEqual(3, decoder.Proccessed);
		}

		[Test]
		public void DtmfDecoderTest5()
		{
			byte[][] pack1 = new byte[][] 
			{ 
				server_A_1_1, server_A_1_2, server_A_1_3,		
				server_B_1_4, server_B_1_5, server_B_1_6, server_B_1_7, server_B_1_8, server_B_1_9,		
				server_A_1_10, server_A_1_11,
			};
			var decoder2 = ProcessEach(pack1);
			var decoder1 = ProcessAll(pack1);

			Assert.AreEqual(2, decoder1.DtmfCodes.Count, "ProcessAll => DtmfCodes.Count");
			Assert.AreEqual((byte)1, decoder1.DtmfCodes[0].Code, "ProcessAll => DtmfCodes[0].Code");
			Assert.AreEqual((byte)1, decoder1.DtmfCodes[1].Code, "ProcessAll => DtmfCodes[1].Code");
			Assert.AreEqual(9, decoder1.Proccessed);

			Assert.AreEqual(2, decoder2.DtmfCodes.Count, "ProcessEach => DtmfCodes.Count");
			Assert.AreEqual((byte)1, decoder2.DtmfCodes[0].Code, "ProcessEach => DtmfCodes[0].Code");
			Assert.AreEqual((byte)1, decoder2.DtmfCodes[1].Code, "ProcessEach => DtmfCodes[1].Code");
			Assert.AreEqual(9, decoder2.Proccessed);
		}

		[Test]
		public void DtmfDecoderTest6()
		{
			byte[][] pack1 = new byte[][] 
			{
				// порядок востановиться в JitterBuffer
				client_2_1, client_1_1,
			};
			var decoder1 = ProcessAll(pack1);

			Assert.AreEqual(2, decoder1.DtmfCodes.Count, "ProcessAll => DtmfCodes.Count");
			Assert.AreEqual((byte)1, decoder1.DtmfCodes[0].Code, "ProcessAll => DtmfCodes[0].Code");
			Assert.AreEqual((byte)2, decoder1.DtmfCodes[1].Code, "ProcessAll => DtmfCodes[1].Code");

			byte[][] pack2 = new byte[][] 
			{
				// порядок востановиться в DtmfDecoder
				client_2_1_x, client_1_1,
			};
			var decoder2 = ProcessAll(pack2);

			Assert.AreEqual(2, decoder2.DtmfCodes.Count, "ProcessEach => DtmfCodes.Count");
			Assert.AreEqual((byte)1, decoder2.DtmfCodes[0].Code, "ProcessEach => DtmfCodes[0].Code");
			Assert.AreEqual((byte)2, decoder2.DtmfCodes[1].Code, "ProcessEach => DtmfCodes[1].Code");
		}

		[Test]
		public void DtmfDecoderTest7()
		{
			byte[][] pack1 = new byte[][] 
			{
				// Timestamp: 115840423 (0x6E795A7) 14480,052875 seconds
				client_1_1, 
				// Timestamp: 115873063 (0x6E81527) 14484,132875 seconds
				client_2_1,
			};

			DtmfDecoder decoder = new DtmfDecoder(106, 8000);
			foreach (var message in pack1)
				decoder.EnqueueMessage(message, 0, message.Length);

			decoder.DelayMilliseconds = 30000;
			decoder.Process();
			Assert.AreEqual(0, decoder.Proccessed);

			decoder.DelayMilliseconds = 0;
			decoder.Process();
			Assert.AreEqual(1, decoder.Proccessed);

			System.Threading.Thread.Sleep(1000);
			decoder.Process();
			Assert.AreEqual(1, decoder.Proccessed);

			System.Threading.Thread.Sleep(4000);
			decoder.Process();
			Assert.AreEqual(2, decoder.Proccessed);

			Assert.AreEqual(2, decoder.DtmfCodes.Count);
			Assert.AreEqual(1, decoder.DtmfCodes[0].Code);
			Assert.AreEqual(2, decoder.DtmfCodes[1].Code);
		}

		[Test]
		public void DtmfDecoderTest8()
		{
			byte[][] pack1 = new byte[][] 
			{
				// Timestamp: 115840423 (0x6E795A7) 14480,052875 seconds
				client_1_1, 
				// Timestamp: 115873063 (0x6E81527) 14484,132875 seconds
				client_2_1,
			};

			DtmfDecoder decoder = new DtmfDecoder(106, 8000);
			foreach (var message in pack1)
				decoder.EnqueueMessage(message, 0, message.Length);

			decoder.DelayMilliseconds = 1000;
			decoder.Process();
			Assert.AreEqual(0, decoder.Proccessed);

			decoder.DelayMilliseconds = 0;
			decoder.Process();
			Assert.AreEqual(1, decoder.Proccessed);

			System.Threading.Thread.Sleep(1000);
			decoder.Process();
			Assert.AreEqual(1, decoder.Proccessed);

			System.Threading.Thread.Sleep(4000);
			decoder.Process();
			Assert.AreEqual(2, decoder.Proccessed);

			Assert.AreEqual(2, decoder.DtmfCodes.Count);
			Assert.AreEqual(1, decoder.DtmfCodes[0].Code);
			Assert.AreEqual(2, decoder.DtmfCodes[1].Code);
		}

		[Test]
		public void DtmfDecoderTest9()
		{
			byte[][] pack1 = new byte[][] 
			{
				client_rtcp_0, client_1_1, client_2_1,
			};
			var decoder1 = ProcessAll(pack1);

			Assert.AreEqual(2, decoder1.DtmfCodes.Count);
			Assert.AreEqual(1, decoder1.DtmfCodes[0].Code);
			Assert.AreEqual(2, decoder1.DtmfCodes[1].Code);
			
			UInt32 rtpTimestamp1 = 0x06E795A7U;
			UInt32 rtpTimestamp2 = 0x06E81527U;
			UInt32 rate = 8000;

			Assert.AreEqual(rtpTimestamp1, decoder1.DtmfCodes[0].RtpTimestamp);
			Assert.AreEqual(rtpTimestamp2, decoder1.DtmfCodes[1].RtpTimestamp);

			UInt64 baseNtpTimestamp = 0xCEA3F3344AE04C05UL;
			UInt32 baseRtpTimestamp = 0x06E04447;

			// rtcp message was proccessed - ntp timestamps are valid
			Assert.AreEqual(baseNtpTimestamp + (((UInt64)(rtpTimestamp1 - baseRtpTimestamp) << 32) / rate),
				decoder1.DtmfCodes[0].NtpTimestamp);
			Assert.AreEqual(baseNtpTimestamp + (((UInt64)(rtpTimestamp2 - baseRtpTimestamp) << 32) / rate),
				decoder1.DtmfCodes[1].NtpTimestamp);
		}

		#region ProcessAll, ProcessEach

		private DtmfDecoder ProcessAll(byte[][] pack)
		{
			DtmfDecoder decoder = new DtmfDecoder(106, 8000)
			{
				DelayMilliseconds = -600000,
			};

			foreach (var message in pack)
				decoder.EnqueueMessage(message, 0, message.Length);

			decoder.Process();

			return decoder;
		}

		private DtmfDecoder ProcessEach(byte[][] pack)
		{
			DtmfDecoder decoder = new DtmfDecoder(106, 8000)
			{
				DelayMilliseconds = -600000,
			};

			foreach (var message in pack)
			{
				decoder.EnqueueMessage(message, 0, message.Length);
				decoder.Process();
			}

			return decoder;
		}

		#endregion
	}
}

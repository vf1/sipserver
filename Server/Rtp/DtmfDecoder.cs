using System;
using System.Collections.Generic;
using Rtp.Payload;
using System.NtpTimestamp;

namespace Rtp
{
	class DtmfDecoder
	{
		#region class DtmfCode

		public class DtmfCode
		{
			public UInt32 SsrcId { get; set; }
			public UInt32 RtpTimestamp { get; set; }

			public UInt64 NtpTimestamp { get; set; }
			public byte Code { get; set; }
			public int Duration { get; set; }
		}

		#endregion

		private RtpReceiver rtpReceiver;
		private int dtmfEventPayloadType;

		public DtmfDecoder(int dtmfEventPayloadType1, int dtmfEventRate1)
		{
			dtmfEventPayloadType = dtmfEventPayloadType1;

			rtpReceiver = new RtpReceiver(new int[] { dtmfEventPayloadType, }, new int[] { dtmfEventRate1 });
			DtmfCodes = new List<DtmfCode>();
			DelayMilliseconds = 120;
		}

		public List<DtmfCode> DtmfCodes
		{
			get;
			private set;
		}

		public int Proccessed
		{
			get;
			private set;
		}

		public int DelayMilliseconds
		{
			get;
			set;
		}

		public void EnqueueMessage(byte[] bytes, int startIndex, int length)
		{
			if (RtpMessage.IsRtpMessage(bytes, startIndex, length))
			{
				var rtpMessage = RtpMessage.Parse(bytes, startIndex, length);

				if (rtpMessage.PayloadType == dtmfEventPayloadType)
				{
					rtpMessage.Payload = DtmfEventMessage.Parse(bytes,
						rtpMessage.GetPayloadOffset(startIndex), length);
				}

				rtpReceiver.InsertMessage(rtpMessage);
			}
			else if (RtcpMessage.IsRtcpMessage(bytes, startIndex, length))
			{
				rtpReceiver.ProcessMessages(
					RtcpMessage.Parse(bytes, startIndex, length));
			}
		}

		protected DtmfCode FindDtmfCode(UInt32 ssrcId, UInt32 timestamp)
		{
			for (int i = 0; i < DtmfCodes.Count; i++)
			{
				if (DtmfCodes[i].RtpTimestamp == timestamp && DtmfCodes[i].SsrcId == ssrcId)
					return DtmfCodes[i];
			}
		
			return null;
		}

		protected void AddDtmfCode(DtmfCode newDtmfCode)
		{
			int index = DtmfCodes.FindIndex((dtmfCode) => { return newDtmfCode.NtpTimestamp > dtmfCode.NtpTimestamp; });

			DtmfCodes.Insert(index + 1, newDtmfCode);
		}

		public void Process()
		{
			foreach (var ssrc in rtpReceiver.Ssrcs)
			{
				var maxTimestamp = ssrc.GetCurrentNtpTimestamp(-DelayMilliseconds);

				for (RtpMessage rtpMessage; ssrc.JitterBuffers[dtmfEventPayloadType].DequeueAvailable(maxTimestamp, out rtpMessage); )
				{
					var dtmfEventMessages = rtpMessage.GetPayload<DtmfEventMessage[]>();

					foreach (var dtmfEventMessage in dtmfEventMessages)
					{
						var dtmfCode = rtpMessage.Marker ? null : FindDtmfCode(ssrc.SsrcId, rtpMessage.Timestamp);

						if (dtmfCode != null)
						{
							dtmfCode.Duration = Math.Max(dtmfCode.Duration, dtmfEventMessage.Duration);
						}
						else
						{
							AddDtmfCode(
								new DtmfCode()
								{
									SsrcId = ssrc.SsrcId,
									RtpTimestamp = rtpMessage.Timestamp,
									NtpTimestamp = rtpMessage.NtpTimestamp,
									Code = dtmfEventMessage.Event,
									Duration = dtmfEventMessage.Duration,
								});
						}
					}

					Proccessed++;
				}
			}
		}
	}
}

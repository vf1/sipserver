using System;
using System.Collections.Generic;
using System.NtpTimestamp;

namespace Rtp
{
	class RtpReceiver
	{
		private List<Ssrc> ssrcs = new List<Ssrc>();
		private int[] payloadTypes;
		private int[] payloadRates;

		public RtpReceiver(int[] payloadTypes1, int[] payloadRates1)
		{
			ssrcs = new List<Ssrc>();
			payloadTypes = payloadTypes1;
			payloadRates = payloadRates1;
		}

		public IEnumerable<Ssrc> Ssrcs
		{
			get
			{
				foreach (var ssrc in ssrcs)
					yield return ssrc;
			}
		}

		public Ssrc FindSsrc(UInt32 ssrcId)
		{
			foreach (var ssrc in ssrcs)
				if (ssrc.SsrcId == ssrcId)
					return ssrc;

			return null;
		}

		protected Ssrc AddSsrc(UInt32 ssrcId, UInt64 ntpTimestamp, UInt32 rtpTimestamp)
		{
			var ssrc = new Ssrc(payloadTypes, payloadRates)
			{
				SsrcId = ssrcId,
			};
			ssrc.SetBaseTimestamp(ntpTimestamp, rtpTimestamp);

			ssrcs.Add(ssrc);

			return ssrc;
		}

		public void InsertMessage(RtpMessage message)
		{
			var ssrc = FindSsrc(message.Ssrc);
			if (ssrc == null)
				ssrc = AddSsrc(message.Ssrc, 0x000fffffffffffff, message.Timestamp);

			ssrc.InsertMessage(message);
		}

		public void ProcessMessages(IEnumerable<RtcpMessage> messages)
		{
			foreach (var message in messages)
			{
				if (message.MessageType == RtcpMessageType.SenderReport)
				{
					var ssrc = FindSsrc(message.SenderReport.Ssrc);

					if (ssrc == null)
						ssrc = AddSsrc(message.SenderReport.Ssrc, message.SenderReport.NtpTimestamp, message.SenderReport.RtpTimestamp);
					else
						ssrc.SetBaseTimestamp(message.SenderReport.NtpTimestamp, message.SenderReport.RtpTimestamp);
				}
			}
		}
	}
}

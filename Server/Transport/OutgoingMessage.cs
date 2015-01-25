//using System;
//using System.Net;
//using System.Net.Sockets;
//using Sip.Message;
//using SocketServers;

//namespace Sip.Server
//{
//    class OutgoingMessage
//    {
//        public IncomingMessageEx(IncomingMessage source, int transactionId)
//        {
//            TransactionId = transactionId;

//            ConnectionAddresses = source.ConnectionAddresses;

//            Reader = source.Reader;

//            Headers = source.Headers;
//            Content = source.Content;
//        }

//        public readonly int TransactionId;

//        public readonly ConnectionAddresses ConnectionAddresses;

//        public readonly SipMessageReader Reader;
//        public readonly ArraySegment<byte> Headers;
//        public readonly ArraySegment<byte> Content;
//    }
//}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SocketServers;

namespace Sip.Server
{
    static class Tracer
    {
        private static string path;
        private static Logger logger;
        private static Timer timer;
        private static EventLog eventLog;

        static Tracer()
        {
            eventLog = new EventLog();
            eventLog.Source = @"OfficeSIP Server";
        }

        public static void Initialize(Logger logger1)
        {
            logger = logger1;
        }

        public static void Configure(string path1, bool isEnabled)
        {
            if (logger.IsEnabled != isEnabled)
            {
                if (isEnabled)
                {
                    path = path1;
                    if (Directory.Exists(path) == false)
                        Directory.CreateDirectory(path);

                    var fileName = path + @"OfficeSIP-Server-" + DateTime.Now.ToString("s").Replace(@":", @"-") + @".pcap";

                    logger.Enable(File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read));

                    timer = new Timer(Flush_Timer, null, 1000, 4000);
                }
                else
                {
                    if (timer != null)
                        timer.Dispose();
                    logger.Disable();
                }
            }
        }

        public static void WriteImportant(string text)
        {
            Write(TraceEventType.Information, text, true);
        }

        public static void WriteInformation(string text)
        {
            Write(TraceEventType.Information, text);
        }

        public static void WriteError(string text)
        {
            Write(TraceEventType.Error, text);
        }

        public static void WriteException(string text, Exception e)
        {
            Write(TraceEventType.Error, text + "\r\n\r\n" + e.Message + "\r\n\r\n" + e.StackTrace + "\r\n");
        }

        private static void Write(TraceEventType type, string message)
        {
            Write(type, message, false);
        }

        private static void Write(TraceEventType type, string message, bool important)
        {
            if (type == TraceEventType.Error || important)
                WriteToEventLog(ToEventLogEntryType(type), message);

            WriteToLogger(type, message);
        }

        private static void WriteToLogger(TraceEventType eventType, string message)
        {
            try
            {
                if (logger != null)
                    logger.WriteComment(eventType.ToString() + ": " + message);
            }
#if DEBUG
            catch (Exception ex)
            {
                throw new Exception("Failed to write message", ex);
            }
#else
            catch
            {
            }
#endif
        }

        private static void WriteToEventLog(EventLogEntryType entryType, string message)
        {
            try
            {
                eventLog.WriteEntry(message, entryType);
            }
            catch
            {
            }
        }

        private static EventLogEntryType ToEventLogEntryType(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Error:
                case TraceEventType.Critical:
                    return EventLogEntryType.Error;

                case TraceEventType.Warning:
                    return EventLogEntryType.Warning;

                default:
                    return EventLogEntryType.Information;
            }
        }

        private static void Flush_Timer(object state)
        {
            if (logger != null)
                logger.Flush();
        }
    }
}

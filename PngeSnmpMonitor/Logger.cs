using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace PngeSnmpMonitor
{
    public class Logger
    {
        private const int LOG_ENTRIES_COUNT = 1000;
        private static Logger instance;

        private static readonly object LogLocker = new object();

        private readonly SynchronizationContext uiContext;

        public Logger()
        {
            uiContext = SynchronizationContext.Current;
        }

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();

                return instance;
            }
        }

        public static ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

        public void AddMessage(Device device, string message)
        {
            lock (LogLocker)
            {
                var timeStamp = DateTime.Now.ToLongTimeString() + "." + DateTime.Now.Millisecond;

                uiContext.Send(x =>
                {
                    LogEntries.Add(new LogEntry(device, message));
                    if (LogEntries.Count > LOG_ENTRIES_COUNT)
                        LogEntries.Remove(LogEntries.First());
                }, null);
            }
        }

        public void AddMessage(Device device, ControlParameter controlParameter, string message)
        {
            lock (LogLocker)
            {
                var timeStamp = DateTime.Now.ToLongTimeString() + "." + DateTime.Now.Millisecond;

                uiContext.Send(x => LogEntries.Add(new LogEntry(device, controlParameter, message)), null);
            }
        }

        public class LogEntry
        {
            public LogEntry(Device device, string message)
            {
                TimeStamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
                DeviceName = device.DeviceName;
                DeviceIP = device.DeviceIP;
                ParameterName = "";
                Message = message;
            }

            public LogEntry(Device device, ControlParameter controlParameter, string message)
            {
                TimeStamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
                DeviceName = device.DeviceName;
                DeviceIP = device.DeviceIP;
                ParameterName = controlParameter.ParameterName;
                Message = message;
            }

            public string TimeStamp { get; set; }

            public string DeviceName { get; set; }

            public string DeviceIP { get; set; }

            public string ParameterName { get; set; }

            public string Message { get; set; }
        }
    }
}
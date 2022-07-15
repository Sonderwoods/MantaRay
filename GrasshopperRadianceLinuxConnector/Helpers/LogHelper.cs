using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    public class LogHelper
    {
        public static Dictionary<string, LogHelper> AllLogSystems = new Dictionary<string, LogHelper>();
        readonly List<LogEntry> logMessages = new List<LogEntry>();
        public string Name;

        object _lock = new object();

        public event EventHandler LogUpdated;

        public static LogHelper Default { get => GetLogHelper(); }

        public static LogHelper GetLogHelper(string name = null)
        {
            name = name ?? ConstantsHelper.ProjectName;
            if ((name == ConstantsHelper.ProjectName && !AllLogSystems.ContainsKey(ConstantsHelper.ProjectName))
               || name == null
               || String.IsNullOrEmpty(name))
            {
                return CreateLogHelper();
            }
            else
            {
                return AllLogSystems[name];
            }
        }



        public List<string> GetLatestLogs(int number = 10, string filter = null)
        {
            List<string> msgs = new List<string>(number);
            IEnumerable<LogEntry> items;

            if (!string.IsNullOrEmpty(filter))
            {
                items = logMessages.OrderByDescending(lo => lo.Timestamp)
                    .Where(l => l.Name.Contains(filter) || l.Description.Contains(filter))
                    .Take(number);
            }
            else
            {
                items = logMessages.OrderByDescending(lo => lo.Timestamp)
                    .Take(number);
            }

            foreach (LogEntry l in items)
            {
                msgs.Add($"[{l.Timestamp:G}, {l.Name}]: {l.Description.Replace("\n", "        \n")}");
            }


            return msgs;
        }


        public Dictionary<string, LogHelper> LogHelpers { get; set; }

        public static LogHelper CreateLogHelper(string name = null, bool overwrite = false)
        {
            name = name ?? ConstantsHelper.ProjectName;
            LogHelper logHelper = new LogHelper()
            {
                Name = name
            };

            if (overwrite)
            {
                AllLogSystems.Remove(name);
                AllLogSystems.Add(name, logHelper);
            }
            else if (!AllLogSystems.ContainsKey(name))
            {
                AllLogSystems.Add(name, logHelper);
            }
            else
            {
                return AllLogSystems[name];
            }

            return logHelper;
        }

        public void Add(string name, string description, Guid guid = default)
        {
            lock (_lock)
            {
                logMessages.Add(
                new LogEntry()
                {
                    Name = name,
                    Description = description,
                    Guid = guid,
                    Timestamp = DateTime.Now
                });
                LogUpdated?.Invoke(this, new EventArgs());
            }

        }

        public void CLear()
        {
            logMessages.Clear();
            LogUpdated?.Invoke(this, new EventArgs());
        }

        public class LogEntry
        {
            public string Name;
            public string Description;
            public DateTime Timestamp;
            public Guid Guid;
        }
    }
}

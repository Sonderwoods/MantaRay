using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MantaRay.Helpers;
using MantaRay.Setup;

namespace MantaRay
{
    public class LogHelper
    {
        public static Dictionary<string, LogHelper> AllLogSystems = new Dictionary<string, LogHelper>();
        readonly List<LogEntry> logMessages = new List<LogEntry>();
        Dictionary<Guid, LogEntry> currentTasks { get; set; } = new Dictionary<Guid, LogEntry>();
        public string Name;
        readonly object _logLock = new object();
        readonly object _taskLock = new object();

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


        public IEnumerable<string> GetCurrentTasks(int number = 10, string nameFilter = null, string descFilter = null)
        {
            List<string> msgs = new List<string>(number);

            foreach (var task in currentTasks.Keys.ToArray()) //toArray ensures a copy so we can edit the original dictionary
            {
                GH_Template_Async comp = Grasshopper.Instances.ActiveCanvas.Document.Objects.OfType<GH_Template_Async>()
                    .Where(o => o.InstanceGuid == currentTasks[task].ComponentGuid).FirstOrDefault();

                if (comp == null || comp.Tasks.Count == 0)
                {
                    TryFinishTask(task, "Error or cancelled??");
                    //currentTasks.Remove(task);
                }

            }

            var items = currentTasks.Values.OrderByDescending(lo => lo.Timestamp)
                .Where(l => (nameFilter == null || l.Name.Contains(nameFilter)) && (descFilter == null || l.Description.Contains(descFilter)))
                .Take(number);


            foreach (LogEntry l in items)
            {
                yield return $"[{l.Timestamp:G}, {l.Name}, for {(DateTime.Now - l.Timestamp).ToReadableString()}]:\n{l.Description.Replace("\n", "        \n")}";
            }


        }

        public Guid AddTask(string name, string description, Guid componentGuid = default, Guid guid = default)
        {
            if (guid == Guid.Empty)
            {
                guid = Guid.NewGuid();
            }
            lock (_taskLock)
            {
                currentTasks.Add(
                    guid,
                    new LogEntry()
                    {
                        Name = name,
                        Description = description,
                        ComponentGuid = componentGuid,
                        Guid = guid,
                        Timestamp = DateTime.Now
                    });
                LogUpdated?.Invoke(this, new EventArgs());
            }

            return guid;

        }

        public bool TryFinishTask(Guid taskGuid, string status = "Finished")
        {

            if (currentTasks.ContainsKey(taskGuid) && taskGuid != Guid.Empty)
            {
                lock (_taskLock)
                {
                    if (currentTasks.ContainsKey(taskGuid))
                    {

                        var t = currentTasks[taskGuid];
                        Add(t.Name, t.Description + $"\n{status} in {(DateTime.Now - t.Timestamp).ToReadableString()}", t.ComponentGuid);
                        currentTasks.Remove(taskGuid);
                        LogUpdated?.Invoke(this, new EventArgs());
                        return true;
                    }
                    else
                        return false;
                }
            }
            else
            {
                Debug.WriteLine($"Tried to remove {taskGuid} from currentTasks without luck");
                return false;
            }

        }


        public IEnumerable<string> GetLatestLogs(int number = 10, string nameFilter = null, string descFilter = null)
        {
            List<string> msgs = new List<string>(number);
            IEnumerable<LogEntry> items;

            items = logMessages.OrderByDescending(lo => lo.Timestamp)
                .Where(l => (nameFilter == null || l.Name.Contains(nameFilter)) && (descFilter == null || l.Description.Contains(descFilter)))
                .Take(number);

            foreach (LogEntry l in items)
            {
                yield return $"[{l.Timestamp:G}, {l.Name}]:\n{l.Description.Replace("\n", "        \n")}";
            }

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
            lock (_logLock)
            {
                logMessages.Add(
                new LogEntry()
                {
                    Name = name,
                    Description = description,
                    ComponentGuid = guid,
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
            public Guid ComponentGuid;
        }
    }
}

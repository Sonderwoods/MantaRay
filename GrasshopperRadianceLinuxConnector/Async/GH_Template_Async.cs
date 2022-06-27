using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace GrasshopperRadianceLinuxConnector
{


    /// <summary>
    /// Inherit your component from this class to make all the async goodness available.
    /// Source: Speckle! https://github.com/specklesystems/GrasshopperAsyncComponent
    /// </summary>
    public abstract class GH_TemplateAsync : GH_Template
    {
        public enum AestheticPhase
        {
            Running,
            NotRunning
        }

        public AestheticPhase PhaseForColors = GH_TemplateAsync.AestheticPhase.NotRunning;

        Stopwatch stopwatch = new Stopwatch();

        public string logDescription;
        public string logName;

        public bool RunInput { get; set; } = true;
        public double RunTime { get; set; }

        public override Guid ComponentGuid => throw new Exception("ComponentGuid should be overriden in any descendant of GH_AsyncComponent!");

        //List<(string, GH_RuntimeMessageLevel)> Errors;

        Action<string, double> ReportProgress;

        public ConcurrentDictionary<string, double> ProgressReports;

        Action Done;

        Timer DisplayProgressTimer;

        int State = 0;

        int SetData = 0;

        public List<WorkerInstance> Workers;

        List<Task> Tasks;

        public readonly List<CancellationTokenSource> CancellationSources;

        public int Pids { get; set; } = -1; // for linux pids

        /// <summary>
        /// Set this property inside the constructor of your derived component. 
        /// </summary>
        public WorkerInstance BaseWorker { get; set; }

        /// <summary>
        /// Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
        /// </summary>
        public TaskCreationOptions? TaskCreationOptions { get; set; } = null;

        protected GH_TemplateAsync(string name, string nickname, string description, string subCategory) : base(name, nickname, description, subCategory)
        {

            DisplayProgressTimer = new Timer(333) { AutoReset = false };
            DisplayProgressTimer.Elapsed += DisplayProgress;

            ReportProgress = (id, value) =>
            {
                ProgressReports[id] = value;
                if (!DisplayProgressTimer.Enabled)
                {
                    DisplayProgressTimer.Start();
                }
            };

            Done = () =>
            {
                Interlocked.Increment(ref State);
                if (State == Workers.Count && SetData == 0)
                {
                    Interlocked.Exchange(ref SetData, 1);

                    // We need to reverse the workers list to set the outputs in the same order as the inputs. 
                    Workers.Reverse();

                    Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                    {
                        ExpireSolution(true);
                    });
                }
                RunTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();

                if (!String.IsNullOrEmpty(logName) || !String.IsNullOrEmpty(logDescription))
                {
                    LogHelper logHelper = LogHelper.Default;
                    logHelper.Add(logName, logDescription + " Done", InstanceGuid);
                }


            };

            ProgressReports = new ConcurrentDictionary<string, double>();

            Workers = new List<WorkerInstance>();
            CancellationSources = new List<CancellationTokenSource>();
            Tasks = new List<Task>();
        }

        public virtual void DisplayProgress(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Workers.Count == 0 || ProgressReports.Values.Count == 0)
            {
                return;
            }

            if (Workers.Count == 1)
            {
                var progress = ProgressReports.Values.Last();
                if (progress == 0)
                {
                    Message = "Running";
                }
                else
                {
                    Message = ProgressReports.Values.Last().ToString("0.00%");

                }
            }
            else
            {
                double total = 0;
                foreach (var kvp in ProgressReports)
                {
                    total += kvp.Value;
                }

                Message = (total / Workers.Count).ToString("0.00%");
            }

            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
            {
                OnDisplayExpired(true);
            });
        }

        protected override void BeforeSolveInstance()
        {
            if (State != 0 && SetData == 1)
            {
                return;
            }

            Debug.WriteLine("Killing");

            foreach (var source in CancellationSources)
            {
                source.Cancel();
            }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref State, 0);
        }

        protected override void AfterSolveInstance()
        {
            Debug.WriteLine("After solve instance was called " + State + " ? " + Workers.Count);
            // We need to start all the tasks as close as possible to each other.
            if (State == 0 && Tasks.Count > 0 && SetData == 0)
            {
                Debug.WriteLine("After solve INVOKATIONM");
                foreach (var task in Tasks)
                {
                    task.Start();
                }
            }
        }

        protected override void ExpireDownStreamObjects()
        {
            // Prevents the flash of null data until the new solution is ready
            if (SetData == 1)
            {
                base.ExpireDownStreamObjects();
            }

            else if (RunInput)
            {
                base.ExpireDownStreamObjects();
            }

        }

        /// <summary>
        /// To override in case you want to change messages, colors or anything else whenever the run is set to false
        /// </summary>
        /// <param name="DA"></param>
        protected virtual void PerformIfInactive(IGH_DataAccess DA)
        {

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            RunInput = true;

            IGH_Param runParam = this.Params.Input.Where(o => o.NickName == "Run" && o.Access == GH_ParamAccess.tree).FirstOrDefault();

            if (runParam != null)
            {
                List<GH_Boolean> runInputs = DA.FetchTree<GH_Boolean>("Run").FlattenData();

                if (runInputs.Count == 0 || !runInputs.All(x => x.Value == true))
                {
                    RunInput = false;
                }
            }

            if (!RunInput)
            {
                RequestCancellation();

                PerformIfInactive(DA);

                return;
            }



            if (State == 0 && RunInput) // Starting up a task
            {
                if (BaseWorker == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                    return;
                }

                var currentWorker = BaseWorker.Duplicate();
                if (currentWorker == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
                    return;
                }

                // Let the worker collect data.
                currentWorker.GetData(DA, Params);

                if (currentWorker.SkipRun)
                {

                    return;
                }

                stopwatch.Start();
                PhaseForColors = AestheticPhase.Running;


                if (!String.IsNullOrEmpty(logName) || !String.IsNullOrEmpty(logDescription))
                {
                    LogHelper logHelper = LogHelper.Default;
                    logHelper.Add(logName, logDescription + " Starting", InstanceGuid);
                }

                // Create the task
                var tokenSource = new CancellationTokenSource();
                currentWorker.CancellationToken = tokenSource.Token;
                currentWorker.Id = $"Worker-{DA.Iteration}";

                var currentRun = TaskCreationOptions != null
                  ? new Task(() => currentWorker.DoWork(ReportProgress, Done), tokenSource.Token, (TaskCreationOptions)TaskCreationOptions)
                  : new Task(() => currentWorker.DoWork(ReportProgress, Done), tokenSource.Token);

                // Add cancellation source to our bag
                CancellationSources.Add(tokenSource);

                // Add the worker to our list
                Workers.Add(currentWorker);

                Tasks.Add(currentRun);

                return;
            }

            if (SetData == 0)
            {
                return;
            }

            if (Workers.Count > 0)
            {
                Interlocked.Decrement(ref State);
                if (State < Workers.Count)
                    Workers[State].SetData(DA);
            }


            if (State != 0)
            {
                return;
            }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref SetData, 0);

            if (RunInput)
            {
                Message = RunTimeFormatted();
                //this.SetPrivateRuntimePropertyValue((int)RunTime);

            }
            else
            {
                Message = "Deactive";
                //this.SetPrivateRuntimePropertyValue(0);

            }

            PhaseForColors = AestheticPhase.NotRunning;


            OnDisplayExpired(true);
        }

        private string RunTimeFormatted()
        {
            if (RunTime > 1000 * 60 * 60)
                return $"Done in {RunTime / 1000.0 / 60.0 / 60.0:0.0}h";
            if (RunTime > 1000 * 60)
                return $"Done in {RunTime / 1000.0 / 60.0:0.0}m";
            else if (RunTime > 1000.0)
                return $"Done in {RunTime / 1000.0:0.0}s";
            else
                return $"Done in {RunTime}ms";
        }

        public virtual void RequestCancellation()
        {
            foreach (var source in CancellationSources)
            {
                source.Cancel();
            }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref State, 0);
            Interlocked.Exchange(ref SetData, 0);
            Message = "Cancelled";
            OnDisplayExpired(true);
            
        }

        public override void CreateAttributes()
        {
            //base.CreateAttributes();
            m_attributes = new GH_ColorAttributes_Async(this);

        }

        public override TimeSpan ProcessorTime => new TimeSpan(0, 0, 0, 0, (int)RunTime);

    }


}

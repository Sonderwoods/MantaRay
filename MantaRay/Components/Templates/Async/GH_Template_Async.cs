using Grasshopper.Kernel;
using MantaRay.Components.Templates;
using Renci.SshNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace MantaRay.Components.Templates.Async
{
    /// <summary>
    /// Inherit your component from this class to make all the async goodness available. (Based on the Speckle component)
    /// </summary>
    public abstract class GH_Template_Async : GH_Template_SaveStrings
    {
        public override Guid ComponentGuid => throw new Exception("ComponentGuid should be overriden in any descendant of GH_AsyncComponent!");

        protected Action<string, double> ReportProgress;

        public ConcurrentDictionary<string, double> ProgressReports;

        /// <summary>
        /// Action to be called when the <see cref="WorkerInstance.DoWork(Action{string, double}, Action)"/> is done.
        /// </summary>
        protected Action Done;

        protected Timer DisplayProgressTimer;

        /// <summary>
        /// State is the number of active workers
        /// </summary>
        protected int State = 0;

        protected int SetData = 0;

        public List<WorkerInstance> Workers;

        public List<Task> Tasks;

        public List<CancellationTokenSource> CancellationSources;

        protected bool firstRun = false;

        /// <summary>
        /// Set this property inside the constructor of your derived component. 
        /// </summary>
        public WorkerInstance BaseWorker { get; set; }

        /// <summary>
        /// Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
        /// </summary>
        public TaskCreationOptions? TaskCreationOptions { get; set; } = null;

        protected GH_Template_Async(string name, string nickname, string description, string subCategory) : base(name, nickname, description, subCategory)
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
                Message = ProgressReports.Values.Last().ToString("0.00%");
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

            base.BeforeSolveInstance();
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
            if (SetData == 1/* && !firstRun*/)
            {
                base.ExpireDownStreamObjects();
                //firstRun = true;
            }
        }

        protected void ForceExpireDownStreamObjects()
        {
            base.ExpireDownStreamObjects();
        }

        ///// <summary>
        ///// To clear lists etc, only running on the first iteration! No need to call base as it is empty in template
        ///// </summary>
        ///// <param name="DA"></param>
        //protected virtual void RunOnlyOnce(IGH_DataAccess DA)
        //{

        //}

        protected override void SolveInstance(IGH_DataAccess DA)
        {



            //return;
            if (State == 0) //State 0 == START RUNNING
            {
                //if (RunCount == 1)
                //{
                //    RunOnlyOnce(DA);
                //}
                if (RunCount == 1)
                {
                    foreach (var source in CancellationSources)
                    {
                        source.Cancel();
                    }
                    CancellationSources.Clear();
                }



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

                if (!PreRunning(DA))
                {
                    PostRunning(DA);
                    return;
                }


                // Create the task
                var tokenSource = new CancellationTokenSource();
                currentWorker.CancellationToken = tokenSource.Token;
                currentWorker.Id = DA.Iteration;


                try
                {
                    Task currentRun = TaskCreationOptions != null
                      ? new Task(() => currentWorker.DoWork(ReportProgress, Done), tokenSource.Token, (TaskCreationOptions)TaskCreationOptions)
                      : new Task(() => currentWorker.DoWork(ReportProgress, Done), tokenSource.Token);

                    // Add cancellation source to our bag
                    CancellationSources.Add(tokenSource);

                    // Add the worker to our list
                    Workers.Add(currentWorker);

                    Tasks.Add(currentRun);
                }
                catch (AggregateException ae)
                {
                    foreach (var item in ae.InnerExceptions)
                    {
                        Debug.Write(item.ToString()); 
                    }
                }

              

                return;
            }

            if (SetData == 0)
            {
                return;
            }

            if (Workers.Count > 0)
            {
                //ClearCachedData(); // <-- to delete persistant data before overriding.
                Interlocked.Decrement(ref State);
                if (State < Workers.Count)
                    Workers[State].SetData(DA);
            }

            if (State != 0)
            {
                return;
            }

            PostRunning(DA);

            //CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref SetData, 0);




            OnDisplayExpired(true);
        }

        /// <summary>
        /// Part of the SolveInstance fired just before the tasks start.
        /// If it returns false, the task will be skipped!
        /// </summary>
        /// <param name="DA"></param>
        /// <returns>Boolean. If false the tasks will be skipped!</returns>
        protected virtual bool PreRunning(IGH_DataAccess DA) => true;

        /// <summary>
        /// Part of the SolveInstance after a done job. This can be used to set color or Message. OR! Setting output values if RUN == false
        /// </summary>
        protected virtual void PostRunning(IGH_DataAccess DA)
        {

            Message = "Done";


        }

        public override void ClearCachedData()
        {
            OldResults = new string[Workers.Count];
        }


        public virtual void RequestCancellation()
        {
            foreach (var source in CancellationSources)
            {
                source.Cancel();
            }

            if (ActiveCommands == null) ActiveCommands = new List<ShellStream>();
            foreach (var cmd in ActiveCommands)
            {
                cmd.WriteLine("\x03");
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

    }
}

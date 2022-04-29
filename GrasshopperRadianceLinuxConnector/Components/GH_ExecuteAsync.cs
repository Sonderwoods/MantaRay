using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Documentation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public class GH_ExecuteAsync : GH_TemplateAsync
    {
        /// <summary>
        /// Initializes a new instance of the GH_Execute class.
        /// </summary>
        public GH_ExecuteAsync()
          : base("Execute SSH Async", "Execute SSH Async",
              "Use me to execute a SSH Command",
              "1 SSH")
        {

            BaseWorker = new SSH_Worker(this);
            Hidden = true;
        }

        public bool FirstRun { get; set; } = true;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SSH Commands", "SSH commands", "SSH commands. Each item in list will be executed", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("stdout", "stdout", "stdout", GH_ParamAccess.list);
            pManager.AddTextParameter("stderr", "stderr", "stderr", GH_ParamAccess.list);
            //pManager.AddBooleanParameter("success", "success", "success", GH_ParamAccess.item);
            pManager.AddTextParameter("log", "log", "log", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Pid", "Pid", "Linux process id. Can be used to kill the task if it takes too long. Simply write in a bash prompt: kill <id>", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran without stderr", GH_ParamAccess.tree); //always keep ran as the last parameter
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Cancel and kill linux", (s, e) =>
            {
                LinuxKill();
                RequestCancellation();
            });
        }

        public void LinuxKill()
        {
            if (pid > 0)
                SSH_Helper.Execute($"kill {pid}", prependSuffix: false);
        }

        //public override void AddedToDocument(GH_Document document)
        //{

        //    base.AddedToDocument(document);
        //}

        public override bool IsPreviewCapable => true;

        public class RunInfo
        {
            public List<string> commands { get; set; } = new List<string>();
            public bool ran { get; set; } = false;
            public int pid { get; set; } = -1;
            public StringBuilder stdout { get; set; } = new StringBuilder();
            public StringBuilder stderr { get; set; } = new StringBuilder();
            public StringBuilder log { get; set; } = new StringBuilder();
            public bool success { get; set; } = false;


        }



        public class SSH_Worker : WorkerInstance
        {

            // Define all the parameters for the component here



            public GH_Structure<GH_String> commands = new GH_Structure<GH_String>();
            public bool run = false;

            public bool ran = false;

            public RunInfo[] results = new RunInfo[0];
            public RunInfo[] savedResults = new RunInfo[0];


            //ConcurrentQueue<string> threadLogs = new ConcurrentQueue<string>();
            //ConcurrentQueue<string> threadStdouts = new ConcurrentQueue<string>();
            //ConcurrentQueue<string> threadErrors = new ConcurrentQueue<string>();
            //ConcurrentQueue<bool> threadRan = new ConcurrentQueue<bool>();
            //ConcurrentQueue<int> pids = new ConcurrentQueue<int>();

            //public List<string> savedStdout = new List<string>();

            public SSH_Worker(GH_Component component) : base(component) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                //((GH_TemplateAsync)Parent).HasEverRun = true;

                DateTime start = DateTime.Now;
                // Checking for cancellation
                if (CancellationToken.IsCancellationRequested) { return; }

                //bool success = false;

                if (run)
                {
                    //((GH_TemplateAsync)Parent).SkipRun = false;

                    ReportProgress(Id, 0);

                    object myLock = new object();
                    results = new RunInfo[commands.Branches.Count];



                    //Parent.Hidden = true;



                    Parallel.For(0, commands.Branches.Count, i =>
                    {

                        RunInfo result = new RunInfo();


                        int pid = -1;
                        //StringBuilder log = new StringBuilder();
                        //StringBuilder stdout = new StringBuilder();
                        //StringBuilder errors = new StringBuilder();

                        //List<GH_String> threadCommands = commands.Branches[i];
                        string command = String.Join(";", commands.Branches[i].Select(c => c.Value)).AddGlobals();

                        pid = SSH_Helper.Execute(command, result.log, result.stdout, result.stderr, prependSuffix: true);

                        // TODO Need to get pid through "beginexecute" instead of "execute" of SSH.

                        bool itsJustAWarning = result.stderr.ToString().Contains("warning");

                        result.success = pid > 0 || itsJustAWarning;

                        if (result.success)
                        {
                            result.pid = pid;
                            //Parent.Message = "Success! pid: " + pid.ToString();
                            //savedStdout = stdout.ToString();
                            //if (itsJustAWarning)
                            //    Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, errors.ToString());
                            result.ran = true;
                            //Parent.Hidden = false;

                        }
                        else
                        {
                            //Parent.Message = "Error :-(";
                            //savedStdout = string.Empty;
                            //Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, errors.ToString());
                            //ran = false;

                        }

                        lock (myLock)
                        {
                            results[i] = result;
                        }



                    });

                    //if (results.Any(r => !r.success))
                    //{
                    //    foreach (string msg in results.Where(r => !r.success).Select(r => r.stderr.ToString()))
                    //    {
                    //        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, msg);
                    //    }

                    //}

                    //collect results


                }
                else //run==false
                {
                    ran = false;
                    //this.Message = "";
                    Parent.Hidden = true;
                    //DA.SetData("stdout", _stdout);
                    if (!savedResults.Any(s => String.IsNullOrEmpty(s.stdout.ToString())))
                    {
                        results = savedResults;
                        //threadStdouts = new ConcurrentQueue<string>();
                        //stdout.Clear();
                        //threadStdouts.Enqueue(savedStdout);
                        //stdout.Append(savedStdout);
                        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old existing stdout\nThis can be convenient for opening old workflows and not running everything again.");
                    }

                }

                Done();
            }

            public override WorkerInstance Duplicate() => new SSH_Worker(Parent);


            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                if (CancellationToken.IsCancellationRequested) return;
                commands = DA.FetchTree<GH_String>(0);

                List<GH_Boolean> _runs = DA.FetchTree<GH_Boolean>(1).FlattenData();
                run = _runs.Count > 0 && _runs.All(g => g.Value == true);

                SkipRun = ((GH_ExecuteAsync)Parent).FirstRun && !run;
                ((GH_ExecuteAsync)Parent).FirstRun = false;

            }

            public override void SetData(IGH_DataAccess DA)
            {
                if (CancellationToken.IsCancellationRequested) return; //Remove?

                //DA.SetData(0, stdout.ToString());
                DA.SetDataList(0, results.Select(r => r.stdout.ToString()));
                //DA.SetData(1, errors.ToString());
                DA.SetDataList(1, results.Select(r => r.stderr.ToString()));
                //DA.SetData(2, log.ToString());
                DA.SetDataList(2, results.Select(r => r.log.ToString()));
                //DA.SetData(3, pid);
                DA.SetDataList(3, results.Select(r => r.pid));
                //DA.SetData(4, ran);
                var runOut = new GH_Structure<GH_Boolean>();
                runOut.Append(new GH_Boolean(run ? results.All(r => r.ran) : false), new GH_Path(0));

                DA.SetDataTree(4, runOut);

                foreach (string msg in results.Where(r => !r.success).Select(r => r.stderr.ToString()))
                {
                    Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, msg);
                }

                //if (CancellationToken.IsCancellationRequested) return;
                //DA.SetData(0, $"Hello world. Worker {Id} has spun for {MaxIterations} iterations.");
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("stdouts", String.Join(">JOIN<", ((SSH_Worker)BaseWorker).results.Select(r => r.stdout)));


            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            string s = String.Empty;
            reader.TryGetString("stdouts", ref s);
            string[] splitString = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None);
            ((SSH_Worker)BaseWorker).savedResults = new RunInfo[splitString.Length];
            for (int i = 0; i < splitString.Length; i++)
            {
                ((SSH_Worker)BaseWorker).savedResults[i] = new RunInfo() { stdout = new StringBuilder(splitString[i]) };
            }




            return base.Read(reader);
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("257C7A8C-330E-43F5-AC62-19F517A3F528"); }
        }
    }
}
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

        public RunInfo[] savedResults = new RunInfo[0];

        public bool FirstRun { get; set; } = true;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SSH Commands", "SSH commands", "SSH commands. Each item in list will be executed", GH_ParamAccess.tree);
            pManager[pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.tree, false)].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("stdout", "stdout", "stdout", GH_ParamAccess.list);
            pManager.AddTextParameter("stderr", "stderr", "stderr", GH_ParamAccess.list);
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
                SSH_Helper.Execute($"kill {pid}", prependPrefix: false);
        }


        public override bool IsPreviewCapable => true;

        public class RunInfo
        {
            public List<string> Commands { get; set; } = new List<string>();
            public bool Ran { get; set; } = false;
            public int Pid { get; set; } = -1;
            public StringBuilder Stdout { get; set; } = new StringBuilder();
            public StringBuilder Stderr { get; set; } = new StringBuilder();
            public StringBuilder Log { get; set; } = new StringBuilder();
            public bool Success { get; set; } = false;


        }



        public class SSH_Worker : WorkerInstance
        {

            // Define all the parameters for the component here

            public GH_Structure<GH_String> commands = new GH_Structure<GH_String>();

            public bool run = false;

            public bool ran = false;

            public RunInfo[] results = new RunInfo[0];


            public SSH_Worker(GH_Component component) : base(component) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                
                if (CancellationToken.IsCancellationRequested) { return; }

                
                if (run)
                {

                    ReportProgress(Id, 0);

                    object myLock = new object();
                    results = new RunInfo[commands.Branches.Count];


                    Parallel.For(0, commands.Branches.Count, i =>
                    {

                        RunInfo result = new RunInfo();

                        int pid = -1;

                        string command = String.Join(";", commands.Branches[i].Select(c => c.Value)).AddGlobals();

                        pid = SSH_Helper.Execute(command, result.Log, result.Stdout, result.Stderr, prependPrefix: true);

                        // TODO Need to get pid through "beginexecute" instead of "execute" of SSH.

                        bool itsJustAWarning = result.Stderr.ToString().Contains("warning");

                        result.Success = pid > 0 || itsJustAWarning;

                        if (result.Success)
                        {
                            result.Pid = pid;
                            result.Ran = true;

                        }

                        lock (myLock)
                        {
                            results[i] = result;
                        }

                    });

                    ((GH_ExecuteAsync)Parent).savedResults = results.ToArray();


                }
                else //run==false
                {
                    ran = false;

                    Parent.Hidden = true;

                    if (((GH_ExecuteAsync)Parent).savedResults.Any(s => !String.IsNullOrEmpty(s.Stdout.ToString())))
                    {
                        results = ((GH_ExecuteAsync)Parent).savedResults;
                    }

                }

                ran = run && results.All(r => r.Ran);

                Done();
            }


            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                //if (CancellationToken.IsCancellationRequested) return;
                commands = DA.FetchTree<GH_String>(0);

                List<GH_Boolean> _runs = DA.FetchTree<GH_Boolean>(1).FlattenData();

                run = _runs.Count > 0 && _runs.All(g => g?.Value == true);

                //SkipRun = ((GH_ExecuteAsync)Parent).FirstRun || !run;  // was AND
                SkipRun = !run;

                ((GH_ExecuteAsync)Parent).FirstRun = false;

            }

            public override void SetData(IGH_DataAccess DA)
            {
                //if (CancellationToken.IsCancellationRequested) return;

                if (run)
                {
                    Parent.Hidden = false;
                }

                if (run == false && ((GH_ExecuteAsync)Parent).savedResults.Any(s => !String.IsNullOrEmpty(s.Stdout.ToString())))
                {
                    Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old existing stdout\nThis can be convenient for opening old workflows and not running everything again.");
                }


                DA.SetDataList(0, results.Select(r => r.Stdout.ToString()));

                DA.SetDataList(1, results.Select(r => r.Stderr.ToString()));

                DA.SetDataList(2, results.Select(r => r.Log.ToString()));

                DA.SetDataList(3, results.Select(r => r.Pid));

                var runOut = new GH_Structure<GH_Boolean>();
                runOut.Append(new GH_Boolean(ran), new GH_Path(0));
                DA.SetDataTree(4, runOut);


                foreach (string msg in results.Where(r => !r.Success).Select(r => r.Stderr.ToString()))
                {
                    Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, msg);
                }

            }

            public override WorkerInstance Duplicate() => new SSH_Worker(Parent);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("stdouts", String.Join(">JOIN<", ((SSH_Worker)BaseWorker).results.Select(r => r.Stdout)));


            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            string s = String.Empty;

            reader.TryGetString("stdouts", ref s);

            string[] splitString = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None);

            savedResults = new RunInfo[splitString.Length];

            for (int i = 0; i < splitString.Length; i++)
            {
                savedResults[i] = new RunInfo() { Stdout = new StringBuilder(splitString[i]) };
            }

            return base.Read(reader);
        }

        public override Guid ComponentGuid => new Guid("257C7A8C-330E-43F5-AC62-19F517A3F528");

    }
}
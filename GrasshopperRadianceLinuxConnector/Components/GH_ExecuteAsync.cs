using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using GH_IO.Serialization;
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
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SSH Commands", "SSH commands", "SSH commands. Each item in list will be executed", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("stdout", "stdout", "stdout", GH_ParamAccess.item);
            pManager.AddTextParameter("stderr", "stderr", "stderr", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("success", "success", "success", GH_ParamAccess.item);
            pManager.AddTextParameter("log", "log", "log", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Pid", "Pid", "Linux process id. Can be used to kill the task if it takes too long. Simply write in a bash prompt: kill <id>", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran without stderr", GH_ParamAccess.tree); //always keep ran as the last parameter
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Cancel", (s, e) =>
            {
                RequestCancellation();
            });
        }

        public override void AddedToDocument(GH_Document document)
        {
            this.Hidden = true;
            base.AddedToDocument(document);
        }

        public override bool IsPreviewCapable => true;





        private class SSH_Worker : WorkerInstance
        {

            // Define all the parameters for the component here

            public List<string> commands = new List<string>();
            public bool run = false;

            public int pid = -1;
            public bool ran = false;
            public StringBuilder log = new StringBuilder();
            public StringBuilder stdout = new StringBuilder();
            public StringBuilder errors = new StringBuilder();

            public string savedStdout = string.Empty;

            public SSH_Worker(GH_Component component) : base(component) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {

                // Checking for cancellation
                if (CancellationToken.IsCancellationRequested) { return; }

                bool success = false;

                if (run)
                {

                    ReportProgress(Id, 0);

                    log.Clear();
                    stdout.Clear();
                    errors.Clear();


                    Parent.Hidden = true;

                    string command = String.Join(";", commands).AddGlobals();

                    // TODO Need to get pid through "beginexecute" instead of "execute" of SSH.

                    pid = SSH_Helper.Execute(command, log, stdout, errors, prependSuffix: true);

                    bool itsJustAWarning = errors.ToString().Contains("warning");

                    success = pid > 0 || itsJustAWarning;

                    if (success)
                    {
                        Parent.Message = "Success! pid: " + pid.ToString();
                        savedStdout = stdout.ToString();
                        if (itsJustAWarning)
                            Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, errors.ToString());
                        ran = true;
                        Parent.Hidden = false;

                    }
                    else
                    {
                        Parent.Message = "Error :-(";
                        savedStdout = string.Empty;
                        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, errors.ToString());
                        ran = false;

                    }


                }
                else //run==false
                {
                    ran = false;
                    //this.Message = "";
                    Parent.Hidden = true;
                    //DA.SetData("stdout", _stdout);
                    if (!String.IsNullOrEmpty(savedStdout))
                    {
                        stdout.Clear();
                        stdout.Append(savedStdout);
                        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old existing stdout\nThis can be convenient for opening old workflows and not running everything again.");
                    }

                }

                Done();
            }

            public override WorkerInstance Duplicate() => new SSH_Worker(Parent);


            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                if (CancellationToken.IsCancellationRequested) return;
                commands = DA.FetchList<string>(0);
                run = DA.Fetch<bool>(1);

            }

            public override void SetData(IGH_DataAccess DA)
            {
                if (CancellationToken.IsCancellationRequested) return;

                DA.SetData(0, stdout.ToString());
                DA.SetData(1, errors.ToString());
                DA.SetData(2, log.ToString());
                DA.SetData(3, pid);
                DA.SetData(4, ran);

                //if (CancellationToken.IsCancellationRequested) return;
                //DA.SetData(0, $"Hello world. Worker {Id} has spun for {MaxIterations} iterations.");
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("stdout", ((SSH_Worker)BaseWorker).savedStdout);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("stdout", ref ((SSH_Worker)BaseWorker).savedStdout);

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
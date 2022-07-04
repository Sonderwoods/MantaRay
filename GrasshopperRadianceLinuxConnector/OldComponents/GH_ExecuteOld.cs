using System;
using System.Collections.Generic;
using System.Text;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperRadianceLinuxConnector.Components;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    [Obsolete]
    public class GH_ExecuteOld : GH_Template
    {
        
        /// <summary>
        /// Initializes a new instance of the GH_Execute class.
        /// </summary>
        ///
        public GH_ExecuteOld()
          : base("Execute SSH", "Execute SSH",
              "Use me to execute a SSH Command",
              "1 SSH")
        {
            
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

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

        public override void AddedToDocument(GH_Document document)
        {
            this.Hidden = true;
            base.AddedToDocument(document);
        }

        public override bool IsPreviewCapable => true;

        private string _stdout = string.Empty;



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool success = false;


            if (DA.Fetch<bool>("Run"))
            {
                this.Hidden = false;

                StringBuilder log = new StringBuilder();
                StringBuilder stdout = new StringBuilder();
                StringBuilder errors = new StringBuilder();
                List<string> commands = DA.FetchList<string>("SSH Commands");
                string command = String.Join(";", commands).AddGlobals();



                int pid = SSH_Helper.Execute(command, log, stdout, errors, prependPrefix: true);

                bool itsJustAWarning = errors.ToString().Contains("warning");

                success = pid > 0 || itsJustAWarning;

                if (success)
                {
                    this.Message = "Success! pid: " + pid.ToString();
                    _stdout = stdout.ToString();
                    if (itsJustAWarning)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, errors.ToString());

                }
                else
                {
                    this.Message = "Error :-(";
                    _stdout = string.Empty;
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, errors.ToString());

                }



                DA.SetData("stdout", stdout);
                DA.SetData("stderr", errors);
                DA.SetData("log", log);
                DA.SetData("Pid", pid);
                //DA.SetData("success", success);

            }
            else //run==false
            {
                this.Message = "";
                this.Hidden = true;
                DA.SetData("stdout", _stdout);
                if (!String.IsNullOrEmpty(_stdout))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old existing stdout\nThis can be convenient for opening old workflows and not running everything again.");

            }

            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>("Run") && success));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("stdout", _stdout);
            //GH_IWriter datastore = writer.CreateChunk("datastore");
            //foreach (string key in store.Keys)
            //{
            //    GH_IWriter chunk = datastore.CreateChunk(key);
            //    store[key].Write(datastore);
            //}
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("stdout", ref _stdout);
            //GH_IReader datastore = reader.FindChunk("datastore");
            //foreach (GH_IReader chunk in datastore.Chunks)
            //{
            //    GH_Structure<IGH_Goo> tree = new GH_Structure<IGH_Goo>();
            //    tree.Read(chunk);
            //    store[chunk.Name] = tree;
            //}
            return base.Read(reader);
        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("257C7A8C-330E-43F5-AC6B-19F517F3F528"); }
        }
    }
}
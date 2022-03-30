using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public class GH_Execute : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_Execute class.
        /// </summary>
        public GH_Execute()
          : base("Execute SSH", "Execute SSH",
              "Use me to execute a SSH Command",
              "SSH")
        {
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
            pManager.AddTextParameter("log", "log", "log", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Fetch<bool>("Run"))
            {
                StringBuilder log = new StringBuilder();
                StringBuilder stdout = new StringBuilder();
                StringBuilder errors = new StringBuilder();
                List<string> commands = DA.FetchList<string>("SSH Commands");
                string command = String.Join(";", commands);

                SSH_Helper.Execute(command, log, stdout, errors, prependSuffix: true);

                DA.SetData("stdout", stdout);
                DA.SetData("stderr", errors);
                DA.SetData("log", log);

            }

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
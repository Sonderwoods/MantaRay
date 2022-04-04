using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_Globals : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_Globals class.
        /// </summary>
        public GH_Globals()
          : base("Setup Globals", "Globals",
              "Sets globals that can be replaced in the ssh commands and in paths",
              "0 Setup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "Keys", "Keys", GH_ParamAccess.list);
            pManager.AddTextParameter("Values", "Values", "Values", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Pairs", "Pairs", "Pairs", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> keys = DA.FetchList<string>("Keys");
            List<string> values = DA.FetchList<string>("Values");
            List<string> outPairs = new List<string>(keys.Count);

            if (keys.Count != values.Count)
            {
                throw new ArgumentOutOfRangeException("The list lengths does not match");
            }

            GlobalsHelper.Globals.Clear();

            for (int i = 0; i < keys.Count; i++)
            {
                GlobalsHelper.Globals.Add(keys[i], values[i]);
                outPairs.Add($"<{keys[i]}> --> {values[i]}");
            }

            DA.SetDataList(0, outPairs);
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A79EFEF6-AFDB-4A5F-8955-A3C51BCF7CE0"); }
        }
    }
}
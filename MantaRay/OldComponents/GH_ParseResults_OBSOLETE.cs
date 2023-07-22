using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay.OldComponents
{
    [Obsolete]
    public class GH_ParseResults : GH_Template
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// Initializes a new instance of the GH_ParseResults class.
        /// </summary>
        public GH_ParseResults()
          : base("ParseAnnualResults", "AnnualResults",
              "Parse ill files to get results per sensor point",
              "3 Results")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Ill file path", "Ill files", "Ill files. Can be linux or windows path", GH_ParamAccess.item);
            pManager[pManager.AddIntegerParameter("schedule[8760 x 0-1]", "schedule[8760 x 0-1]", "Schedule of 1s and 0s for each hour of the year to include", GH_ParamAccess.list, new List<int>())].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Results", "Results", "Results per point", GH_ParamAccess.list);
            pManager.AddTextParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>(this, "Run"))
                return;

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());

            string path = DA.Fetch<string>(this, "Ill file path");

            var lines = sshHelper.ReadFile(path).Split('\n');



            foreach (var line in lines)
            {

            }




        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("558901E1-FCA8-4ED4-B43F-B9C5620D5356"); }
        }
    }
}
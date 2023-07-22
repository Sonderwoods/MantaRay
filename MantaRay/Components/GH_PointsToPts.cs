using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using Rhino.Geometry;
using MantaRay.Helpers;
using System.Globalization;
using Renci.SshNet;
using MantaRay.Components.Templates;

namespace MantaRay.Components
{
    public class GH_PointsToPts : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_PointsToPts class.
        /// </summary>
        public GH_PointsToPts()
          : base("Points To .pts", "Points2pts",
              "Formats list of points and vectors to a pts string. Use this to write it through the execute component\n\nNote that the string is already formatted to radiance units (meters)",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points", "Points\nIn Rhino units. Will automatically be converted to meter in the radiance string!", GH_ParamAccess.list);
            pManager[pManager.AddVectorParameter("Vectors", "Vectors", "Vectors. Default is 0,0,1", GH_ParamAccess.list, new Vector3d(0, 0, 1))].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("pts string", "pts string", "pts string", GH_ParamAccess.list);
            pManager.AddTextParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!CheckIfRunOrUseOldResults(DA, 0, true)) return; //template

            List<Point3d> pts = DA.FetchList<Point3d>(this, "Points");
            List<Vector3d> vects = DA.FetchList<Vector3d>(this, "Vectors");
            StringBuilder ptsFile = new StringBuilder();



            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);


            if (pts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Points");
                return;

            }

            if (pts.Count > 1 && vects.Count > 1 && pts.Count != vects.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "vector count and point count does not match");
                return;
            }

            if (vects.Count == 0)
                vects.Add(new Vector3d(0, 0, 1));


            if (vects.Count == 1)
            {
                vects.Capacity = pts.Count;
                while (vects.Count < pts.Count)
                {
                    vects.Add(vects[0]);
                }
            }


            for (int i = 0; i < pts.Count; i++)
            {
                ptsFile.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5}\r\n", pts[i].X.ToMeter(), pts[i].Y.ToMeter(), pts[i].Z.ToMeter(), vects[i].X, vects[i].Y, vects[i].Z);
            }

            DA.SetData(0, ptsFile.ToString());

            if (RunCount == 1)
            {
                OldResults = new string[Params.Input[0].VolatileData.PathCount];
            }
            if (OldResults != null && OldResults.Length >= RunCount)
            {

                OldResults[RunCount - 1] = ptsFile.ToString();
            }





        }

        protected override Bitmap Icon => Resources.Resources.Ra_Pt_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1FE05399-3EF7-42A4-A811-D7AF23769D48"); }
        }
    }
}
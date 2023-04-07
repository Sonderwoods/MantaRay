using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Grasshopper.Kernel;
using MantaRay.Components;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_PtsViewer : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_PtsViewer class.
        /// </summary>
        public GH_PtsViewer()
          : base("Pts Viewer", "PtsView",
              "Reads a pts file and previews the points. ",
              "2 Radiance")
        {
        }

        Point3d[] pts = new Point3d[0];
        BoundingBox bb = default;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("PtsString", "pts string", "EITHER\n\npoints file (Windows location)\n" +
                "The pts file has to be locally on windows, so use the download component first\n\nOR\n\n" +
                "A string containing the pts content (ie, from 'cat myPts.pts')\n\n" +
                "Note that we are assuming that the units in the pts file is in meters, no matter the units of the Rhino file!", GH_ParamAccess.list);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            int v = pManager.AddPlaneParameter("Points", "pts", "points", GH_ParamAccess.list);
            pManager.HideParameter(v);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> planes = new List<Plane>();
            List<string> ptsFiles = DA.FetchList<string>(this, "PtsString");

            foreach (var ptsFile in ptsFiles)
            {
                if(!(ptsFile.Contains(" ") || ptsFile.Contains("\t")) && System.IO.File.Exists(ptsFile))
                {
                    planes.AddRange(ReadPtsString(System.IO.File.ReadAllText(ptsFile)));
                }
                else
                {
                    planes.AddRange(ReadPtsString(ptsFile));
                }
                
            }



            pts = planes.Select(p => p.Origin).ToArray();
            bb = new BoundingBox(pts);

            DA.SetDataList(0, planes);
            
        }

        public static List<Plane> ReadPtsString(string ptsString)
        {
            List<Plane> planes = new List<Plane>();

            foreach (string line in ptsString.Split('\n').Where(l => !l.StartsWith("#")))
            {

                double[] n = line.Replace('\t', ' ').Split(' ')
                    .Where(s => !String.IsNullOrEmpty(s))
                    .Select(s => double.Parse(s, CultureInfo.InvariantCulture).FromMeter()).ToArray();
                if (n.Length == 6)
                {
                    planes.Add(new Plane(new Point3d(n[0], n[1], n[2]), new Vector3d(n[3], n[4], n[5])));

                }
            }

            return planes;
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (!Locked)
            {
                //this.Attributes.Selected
                //Type t = GetType();
                //Grasshopper.Instances.ActiveCanvas.Document.Objects.OfType<GH_PtsViewer>().Where(o => o.Attributes.Selected)//OfType<IGH_ActiveObject>().Where(o => o)
                args.Display.DrawPoints(pts, Rhino.Display.PointStyle.RoundSimple, 2f, Attributes.Selected ? System.Drawing.Color.ForestGreen : System.Drawing.Color.DarkSlateBlue);
            }
            
            
            base.DrawViewportMeshes(args);
        }

        public override BoundingBox ClippingBox => bb;

        protected override System.Drawing.Bitmap Icon => Resources.Resources.Ra_Pt_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C230B1F8-934E-416E-A264-C1EA7CC63509"); }
        }
    }
}
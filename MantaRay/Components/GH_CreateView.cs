﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using MantaRay.Components;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_CreateView : GH_Template
    {
        public GH_CreateView()
          : base("CreateView", "View",
              "Create a view based on a rhino named view. If no name is inputted, the active viewport will be used.",
              "2 Radiance")
        {
        }

        Point3d[] PointsTo = null;
        Point3d vp = default(Point3d);
        BoundingBox clippingBox = default;
        double length = 1.0;



        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("Viewport", "Viewport", "Viewport name from rhino", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddGenericParameter("Update", "Update", "Update. Input a boolean to me to make sure im updated whenever you fire the run. Optional", GH_ParamAccess.tree)].Optional = true;
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("viewfile content", "viewfile content", "View file content.\nEcho me into a viewfile", GH_ParamAccess.item);
            pManager.AddNumberParameter("ImageRatio", "ImageRatio", "ImageRatio", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int index = Rhino.RhinoDoc.ActiveDoc.NamedViews.FindByName(DA.Fetch<string>("Viewport"));
            Rhino.DocObjects.ViewportInfo vpInfo;



            if (index == -1)
            {

                vpInfo = new Rhino.DocObjects.ViewportInfo(Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport);
                Message = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name;
            }
            else
            {

                vpInfo = Rhino.RhinoDoc.ActiveDoc.NamedViews[index].Viewport;

                Message = DA.Fetch<string>("Viewport");
            }

            vp = vpInfo.CameraLocation;

            Vector3d vu = vpInfo.CameraUp;
            Vector3d vd = vpInfo.CameraDirection;
            vpInfo.GetCameraAngles(out _, out double vv, out double vh);

            length = 10.0.FromMeter();


            PointsTo = vpInfo.GetFarPlaneCorners();
            for (int i = 0; i < PointsTo.Length; i++)
            {
                PointsTo[i] = vp + (PointsTo[i] - vp) / (PointsTo[i] - vp).Length * length;
            }
            System.Drawing.Rectangle port = vpInfo.GetScreenPort();
            DA.SetData(1, port.Width / (double)port.Height);

            /*
             https://floyd.lbl.gov/radiance/digests_html/v2n7.html#VIEW_ANGLES

            The relationship between perspective view angles and image size is
            determined by tangents, i.e.:

	            tan(vh/2)/tan(vv/2) == hres/vres

            Note that the angles must be divided in half (and expressed in radians
            if you use the standard library functions).  If you know what horizontal
            and vertical resolution you want, and you know what horizontal view angle
            you want (and your pixels are square), you can compute the corresponding
            vertical view angle like so:

	            % calc
	            hres = 1024
	            vres = 676
	            vh = 40
	            vv = 180/PI*2 * atan(tan(vh*PI/180/2)*vres/hres)
	            vv
            (resp)	$1=27.0215022
            */


            if (vpInfo.IsPerspectiveProjection)
            {
                if (PointsTo.Length > 2)
                    clippingBox = new BoundingBox(new Point3d[] { PointsTo[0], PointsTo[1], PointsTo[2], vp });

                string output = $"rvu -vtv " +
                $"-vp {vp.X} {vp.Y} {vp.Z} " +
                $"-vd {vd.X} {vd.Y} {vd.Z} " +
                $"-vu {vu.X} {vu.Y} {vu.Z} " +
                $"-vh {vh * 2.0 * 180.0 / Math.PI:0.000} " +
                $"-vv {vv * 2.0 * 180.0 / Math.PI:0.000}";

                DA.SetData(0, output);

            }
            else if (vpInfo.IsParallelProjection)
            {
                throw new NotImplementedException("View type not supported. Please use perspective view or parallel view. For now");
            }
            else
            {
                throw new NotImplementedException("View type not supported. Please use perspective view or parallel view. For now");
            }




        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {

            DrawCamera(vp, PointsTo, this.Attributes.Selected ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed, args);

            base.DrawViewportWires(args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPts">the quad end points. TODO: Change to view angle and auto generate...</param>
        /// <param name="color"></param>
        /// <param name="args"></param>
        public void DrawCamera(Point3d startPt, Point3d[] endPts, System.Drawing.Color color, IGH_PreviewArgs args)
        {
            if (args.Display.Viewport.Name == Message)
                return;
            

            Point3d avgPoint = default;
            if (endPts != null && endPts.Length > 0)
            {
                int[] order = new[] { 0, 1, 3, 2, 0 };
                for (int i = 0; i < order.Length - 1; i++)
                {
                    int cur = order[i];
                    int next = order[i + 1];
                    args.Display.DrawDottedLine(new Line(startPt, endPts[cur]), color);
                    args.Display.DrawLine(endPts[cur], endPts[next], color, 1);
                    avgPoint += endPts[i];

                }

                args.Display.DrawLine(startPt, (avgPoint / endPts.Length), color, 3);
                args.Display.DrawDot(startPt, Message, System.Drawing.Color.Black, this.Attributes.Selected ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            }
        }

        public override BoundingBox ClippingBox => clippingBox;

        protected override Bitmap Icon => Resources.Resources.Ra_Globals_Cam_Icon;

        public override Guid ComponentGuid => new Guid("9E123876-5AFD-42D0-A173-D7875BE25F45");

    }
}
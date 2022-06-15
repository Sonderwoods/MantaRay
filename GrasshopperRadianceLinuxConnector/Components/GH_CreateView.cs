using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
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

            string output = string.Empty;


            if (vpInfo.IsPerspectiveProjection)
            {
                if (PointsTo.Length > 2)
                    clippingBox = new BoundingBox(new Point3d[] { PointsTo[0], PointsTo[1], PointsTo[2], vp });

                output = $"rvu -vtv " +
                $"-vp {vp.X} {vp.Y} {vp.Z} " +
                $"-vd {vd.X} {vd.Y} {vd.Z} " +
                $"-vu {vu.X} {vu.Y} {vu.Z} " +
                $"-vh {vh * 2.0 * 180.0 / Math.PI:0.000} " +
                $"-vv {vv * 2.0 * 180.0 / Math.PI:0.000}";
            }
            else if (vpInfo.IsParallelProjection)
            {
                throw new NotImplementedException("View type not supported. Please use perspective view or parallel view. For now");
            }
            else
            {
                throw new NotImplementedException("View type not supported. Please use perspective view or parallel view. For now");
            }

            

            DA.SetData(0, output);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (args.Display.Viewport.Name == Message)
                return;

            System.Drawing.Color color = this.Attributes.Selected ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed;
            Point3d avgPoint = default(Point3d);
            if (PointsTo != null && PointsTo.Length > 0)
            {
                int[] order = new[] { 0, 1, 3, 2, 0 };
                for (int i = 0; i < order.Length - 1; i++)
                {
                    int cur = order[i];
                    int next = order[i + 1];
                    args.Display.DrawDottedLine(new Line(vp, PointsTo[cur]), color);
                    args.Display.DrawLine(PointsTo[cur], PointsTo[next], color, 1);
                    avgPoint += PointsTo[i];

                }

                args.Display.DrawLine(vp, (avgPoint / PointsTo.Length), color, 3);
                args.Display.DrawDot(vp, Message, System.Drawing.Color.Black, this.Attributes.Selected ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            }

            

            base.DrawViewportWires(args);
        }

        public override BoundingBox ClippingBox => clippingBox;

        public override Guid ComponentGuid => new Guid("9E123876-5AFD-42D0-A173-D7875BE25F45");

    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClipperLib;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_CreateView : GH_Template, IHasDoubleClick
    {
        public GH_CreateView()
          : base("CreateView", "View",
              "Create a view based on a rhino named view. If no name is inputted, the active viewport will be used.",
              "2 Radiance")
        {
        }

        readonly List<Point3d[]> PointsTo = new List<Point3d[]>();
        readonly List<Point3d> Vp = new List<Point3d>();
        private BoundingBox clippingBox = default;
        readonly List<double> Length = new List<double>();
        readonly List<string> Names = new List<string>();




        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("Viewport", "Viewport", "Viewport name from rhino", GH_ParamAccess.list, "")].Optional = true;
            pManager[pManager.AddBooleanParameter("Update", "Update", "Update. Input a boolean to me to make sure im updated whenever you fire the run. Optional", GH_ParamAccess.tree)].Optional = true;
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("viewfile content", "viewfile content", "View file content.\nEcho me into a viewfile", GH_ParamAccess.list);
            pManager.AddNumberParameter("ImageRatio", "ImageRatio", "ImageRatio", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Boolean> _runs = DA.FetchTree<GH_Boolean>(1).FlattenData();
            List<string> outputs = new List<string>();

            bool run = _runs.Count > 0 && _runs.All(g => g?.Value == true);


            PointsTo.Clear();
            Vp.Clear();
            Length.Clear();
            Names.Clear();
            clippingBox = BoundingBox.Unset;
            List<Rectangle> ports = new List<Rectangle>();


            foreach (string _name in DA.FetchList<string>("Viewport"))
            {
                int index = Rhino.RhinoDoc.ActiveDoc.NamedViews.FindByName(_name);
                Rhino.DocObjects.ViewportInfo vpInfo;


                if (index == -1)
                {
                    vpInfo = new Rhino.DocObjects.ViewportInfo(Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport);
                    Names.Add(Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name);
                }
                else
                {
                    vpInfo = Rhino.RhinoDoc.ActiveDoc.NamedViews[index].Viewport;
                    Names.Add(_name);
                }



                var _vp = vpInfo.CameraLocation;
                Vp.Add(_vp);

                Vector3d vu = vpInfo.CameraUp;
                Vector3d vd = vpInfo.CameraDirection;
                vpInfo.GetCameraAngles(out _, out double vv, out double vh);

                double _length = 10.0.FromMeter();
                Length.Add(_length);



                Point3d[] _pointsTo = vpInfo.GetFarPlaneCorners();

                for (int i = 0; i < _pointsTo.Length; i++)
                {
                    _pointsTo[i] = _vp + (_pointsTo[i] - _vp) / (_pointsTo[i] - _vp).Length * _length;
                }

                PointsTo.Add(_pointsTo);

                ports.Add(vpInfo.GetScreenPort());




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
                    if (_pointsTo.Length > 2)
                    {
                        clippingBox.Union(new BoundingBox(new Point3d[] { _pointsTo[0], _pointsTo[1], _pointsTo[2], _vp }));


                    }

                    string output = $"rvu -vtv " +
                    $"-vp {_vp.X} {_vp.Y} {_vp.Z} " +
                    $"-vd {vd.X} {vd.Y} {vd.Z} " +
                    $"-vu {vu.X} {vu.Y} {vu.Z} " +
                    $"-vh {vh * 2.0 * 180.0 / Math.PI:0.000} " +
                    $"-vv {vv * 2.0 * 180.0 / Math.PI:0.000}";

                    outputs.Add(output);


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

            Message = string.Join(", ", Names);

            DA.SetDataList(1, ports.Select(p => p.Width / (double)p.Height));
            DA.SetDataList(0, outputs);





        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            for (int i = 0; i < Vp.Count; i++)
            {
                DrawCamera(Vp[i], PointsTo[i], this.Attributes.Selected ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed, Names[i], args);
            }



            base.DrawViewportWires(args);
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPts">the quad end points. TODO: Change to view angle and auto generate...</param>
        /// <param name="color"></param>
        /// <param name="args"></param>
        public void DrawCamera(Point3d startPt, Point3d[] endPts, System.Drawing.Color color, string name, IGH_PreviewArgs args)
        {
            if (args.Display.Viewport.Name == name)
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
                args.Display.DrawDot(startPt, name, System.Drawing.Color.Black, this.Attributes.Selected ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            }
        }

        public override void CreateAttributes()
        {
            //base.CreateAttributes();
            m_attributes = new GH_DoubleClickAttributes(this);

        }

        public GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            this.ExpireSolution(true);
            return GH_ObjectResponse.Handled;
        }

        public override BoundingBox ClippingBox => clippingBox;

        protected override Bitmap Icon => Resources.Resources.Ra_Globals_Cam_Icon;

        public override Guid ComponentGuid => new Guid("9E123876-5AFD-42D0-A173-D7875BE25F45");

    }
}
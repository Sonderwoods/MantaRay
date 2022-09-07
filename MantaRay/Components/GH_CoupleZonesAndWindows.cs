using System;
using System.Collections.Generic;

using System.Drawing;
using System.Linq;
using Grasshopper.Documentation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using MantaRay.Helpers;
using Rhino.Display;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_CoupleZonesAndWindows : GH_Template
    {
        List<System.Drawing.Color> lineColors = new List<Color>();
        List<Line> lines = new List<Line>();
        BoundingBox bb = BoundingBox.Empty;
        Mesh[][] previewRooms = new Mesh[0][];
        DisplayMaterial[][] previewRoomMaterials = new DisplayMaterial[0][];

        Mesh[][] previewWindows = new Mesh[0][];
        DisplayMaterial[][] previewWindowMaterials = new DisplayMaterial[0][];

        public GH_CoupleZonesAndWindows()
          : base("CoupleZonesAndWindows", "CoupleZonesAndWindows",
              "Attempts to sort windows and zones together. Is scalable and uses rTree.\n\n" +
                "This is relevant for multi phase simulations where you want shoebox models of each room and attach the nearest windows.",
              "2 Radiance")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Window surfaces", "Window surfaces", "Breps representing the windows.", GH_ParamAccess.tree);
            pManager.AddBrepParameter("Simulation Surface", "Simulation Surfaces", "A brep representing the zone. (like a flat one representing the floor)\n" +
                "We will use Brep.NearestPoint to couple it with windows.\n" +
                "If you have tall windows you could 'tweak' your brep and add height to it etc.\n" +
                "However each window can (as of now) max have 1 attached surface each.", GH_ParamAccess.tree);
            pManager[pManager.AddNumberParameter("tol1", "tol1", "Tol1, set this to the max distance between window and the zone.\n" +
                "if the windows connect to wrong zones, try setting this lower.\n" +
                "Default is 0.1 (m)", GH_ParamAccess.item, 0.1)].Optional = true;
            pManager[pManager.AddNumberParameter("tol2", "tol2", "Tol2, rTree tolerance in document units. You may reach higher performance if you tweak it.\n" +
                "If you set it too high, you may see poor performance when you scale to many rooms.\n" +
                "Set it to -1 for auto (default).", GH_ParamAccess.item, -1)].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            int a = pManager.AddBrepParameter("Window Surface", "Window Surface", "Windows", GH_ParamAccess.tree);
            int b = pManager.AddBrepParameter("Simulation Surface", "Simulation Surface", "Rooms", GH_ParamAccess.tree);
            pManager.AddTextParameter("Paths per window", "Window Paths", "Paths per window.", GH_ParamAccess.list);

            pManager.HideParameter(a);
            pManager.HideParameter(b);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var grids = DA.FetchTree<GH_Brep>(0);
            var windows = DA.FetchTree<GH_Brep>(1);
            double tol1 = DA.Fetch<double>("tol1");
            double tol2 = DA.Fetch<double>("tol2");

            if (tol1 == 0)
                tol1 = 0.1.FromMeter();

            if (grids.PathCount != windows.PathCount)
                throw new Exception("Path counts do not match");

            GH_Structure<GH_Brep> outGrids = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Brep> outWindows = new GH_Structure<GH_Brep>();
            GH_Structure<GH_String> outPaths = new GH_Structure<GH_String>();

            lines.Clear();
            lineColors.Clear();
            previewRooms = new Mesh[grids.PathCount][];
            previewRoomMaterials = new DisplayMaterial[grids.PathCount][];

            previewWindows = new Mesh[grids.PathCount][];
            previewWindowMaterials = new DisplayMaterial[grids.PathCount][];


            for (int i = 0; i < grids.PathCount; i++)
            {

                List<GH_Brep> _grids = ((List<GH_Brep>)grids.get_Branch(i));

                List<GH_Brep> _windows = (List<GH_Brep>)windows.get_Branch(i);

                previewRooms[i] = new Mesh[_grids.Count];
                previewRoomMaterials[i] = new DisplayMaterial[_grids.Count];

                previewWindows[i] = new Mesh[_windows.Count];
                previewWindowMaterials[i] = new DisplayMaterial[_windows.Count];


                Point3d[] windowPts = _windows.Select(w => AreaMassProperties.Compute(w.Value, true, true, false, false).Centroid).ToArray();
                Point3d[] roomPts = _grids.Select(w => AreaMassProperties.Compute(w.Value, true, true, false, false).Centroid).ToArray();

                int[] roomsPerWindow = Helpers.RTreeHelper.ConnectPointsToBreps(windowPts, _grids.Select(g => g.Value).ToList(), tol1, tol2, false);

                List<Point3d> roomCenters = new List<Point3d>();
                List<List<Point3d>> windowCentersPerRoom = new List<List<Point3d>>();



                for (int j = 0; j < _grids.Count; j++)
                {
                    GH_Path gridPath = new GH_Path(grids.Paths[i]).AppendElement(j);
                    outGrids.Append(_grids[j], gridPath);
                    roomCenters.Add(roomPts[j]);
                    windowCentersPerRoom.Add(new List<Point3d>());
                    previewRooms[i][j] = Mesh.CreateFromBrep(_grids[j].Value, MeshingParameters.Default).FirstOrDefault();
                    previewRoomMaterials[i][j] = new DisplayMaterial(ColorHelper.GetRandomColor(), 0);

                }



                for (int j = 0; j < _windows.Count; j++)
                {
                    GH_Path windowPath = new GH_Path(grids.Paths[i]).AppendElement(roomsPerWindow[j]);
                    outWindows.Append(_windows[j], windowPath);

                    GH_Path windowPathPath = new GH_Path(grids.Paths[i]);
                    outPaths.Append(new GH_String(windowPath.ToString()), windowPathPath);

                    lines.Add(new Line(windowPts[j], roomPts[roomsPerWindow[j]]));



                    double dotProduct = lines[lines.Count - 1].Direction * _windows[j].Value.Faces[0].NormalAt(0.5, 0.5);
                    if (dotProduct > 0.0)
                    {
                        lineColors.Add(previewRoomMaterials[i][roomsPerWindow[j]].Diffuse);

                    }
                    else
                    {
                        lineColors.Add(System.Drawing.Color.Red);

                        foreach (BrepEdge edge in _windows[j].Value.Edges)
                        {
                            Polyline vertices = edge.ToPolyline(DocumentTolerance(), DocumentAngleTolerance(), 0.1, double.MaxValue).ToPolyline();
                            for (int k = 0; k < vertices.Count - 1; k++)
                            {
                                lines.Add(new Line(vertices[k], vertices[k + 1]));
                                lineColors.Add(System.Drawing.Color.Red);
                            }
                        }
                    }



                    previewWindows[i][j] = Mesh.CreateFromBrep(_windows[j].Value, MeshingParameters.Default).FirstOrDefault();
                    previewWindowMaterials[i][j] = new DisplayMaterial(previewRoomMaterials[i][roomsPerWindow[j]]) { Transparency = 0.2, IsTwoSided = true, BackDiffuse = Color.Black };




                }

                if (bb.IsValid)
                {
                    bb = new BoundingBox(bb.GetCorners().Concat(windowPts).Concat(roomPts));
                }
                else
                {
                    bb = new BoundingBox(windowPts.Concat(roomPts));
                }
            }



            // TODO: Turn the windows inwards against the room.

            DA.SetDataTree(0, outGrids);
            DA.SetDataTree(1, outWindows);
            DA.SetDataTree(2, outPaths);
        }

        public override BoundingBox ClippingBox => bb;

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                args.Display.DrawLine(lines[i], lineColors[i], 2);
                //args.Display.DrawPatternedLine(line, Color.Brown, 0x001000111, 2);

            }


            for (int i = 0; i < previewRooms.Length; i++)
            {
                for (int j = 0; j < previewRooms[i].Length; j++)
                {
                    args.Display.DrawMeshShaded(previewRooms[i][j], previewRoomMaterials[i][j]);
                }
            }

            for (int i = 0; i < previewWindows.Length; i++)
            {
                for (int j = 0; j < previewWindows[i].Length; j++)
                {
                    args.Display.DrawMeshShaded(previewWindows[i][j], previewWindowMaterials[i][j]);
                }
            }

            base.DrawViewportMeshes(args);

        }




        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("AF816254-D107-4A15-9FDD-3DD9A0DC61ED"); }
        }
    }
}
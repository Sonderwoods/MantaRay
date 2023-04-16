using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using MantaRay;
using Rhino.UI.Controls;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Linq;
using MantaRay.Helpers;

namespace Grasshopper_Doodles_Public
{
    /// <summary>
    /// Original (C) Henning Larsen Architects 2019
    /// GPL License
    /// Author: Mathias Sønderskov
    /// https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public
    /// </summary>
    public class GH_CreateGrid : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GhcGrid class.
        /// </summary>
        public GH_CreateGrid()
          : base("Grid Create", "Grid",
              "Grid\nUse this for Grid based analysis.\nBased on https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public",
              "2 Radiance")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary; // puts the grid component in top.

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            // Do not change names. But ordering can be changed.
            pManager[pManager.AddGeometryParameter("Geometry", "Geo", "Input surfaces, meshes or curves", GH_ParamAccess.list)].DataMapping = GH_DataMapping.Graft;
            pManager.AddNumberParameter("GridSize [m]", "S [m]", "Grid size in meters", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Vertical Offset [m]", "VO [m]", "Vertical offset", GH_ParamAccess.item, 0.75);
            pManager.AddNumberParameter("Edge Offset [m]", "EO [m]*", "*Not yet implemented.\nEdge offset", GH_ParamAccess.item, 0.5);
            int ic = pManager.AddBooleanParameter("IsoCurves", "IC?", "Leave default to make it work with the isocurves preview! For grid pixels, set to false.\nUsing vertice points for later using to generate isocurves. If false, it will use center of faces. Default is true.", GH_ParamAccess.item, true);
            pManager[ic].Optional = true;

            //int ps = pManager.AddBooleanParameter("PerfectSquares?*", "PC?", "*Not yet implemented.\nIt's a attempt to do perfect squares, so you dont get extra points at corners.\nhttps://discourse.ladybug.tools/t/none-uniform-grid/2361/11", GH_ParamAccess.item, true);
            //pManager[ps].Optional = true;
            //// TODO: https://discourse.ladybug.tools/t/none-uniform-grid/2361/11

            pManager.AddBooleanParameter("GoLarge", "GoLarge", "Set to true if you accept grids larger than 60000 points. This is a safety check.", GH_ParamAccess.item, false);




        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Grids", "G", "Output grids. Preview these with the GridViewer component", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Meshes", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "Pt", "Simulation points", GH_ParamAccess.list);
            pManager.AddVectorParameter("Normals", "Vect", "Normals", GH_ParamAccess.list);

            //pManager.AddTextParameter("msg", "m", "msg", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var gridSize = UnitHelper.FromMeter(DA.Fetch<double>(this, "GridSize [m]"));
            var edgeOffset = UnitHelper.FromMeter(DA.Fetch<double>(this, "Edge Offset [m]"));

            var offset = UnitHelper.FromMeter(DA.Fetch<double>(this, "Vertical Offset [m]"));
            var useCenters = DA.Fetch<bool>(this, "IsoCurves");
            var geometries = DA.FetchList<IGH_GeometricGoo>(this, "Geometry");
            var goLarge = DA.Fetch<bool>(this, "GoLarge");

            List<GH_Point> centers = new List<GH_Point>();
            List<GH_Vector> normals = new List<GH_Vector>();
            List<Grid> myGrids = new List<Grid>();
            List<GH_Mesh> meshes = new List<GH_Mesh>();

            //List<Mesh> inMeshes = new List<Mesh>();
            List<Brep> inBreps = new List<Brep>();
            List<Curve> inCrvs = new List<Curve>();

            //string msg = "";
            useCenters = !useCenters;

            for (int i = 0; i < geometries.Count; i++)
            {
                if (geometries[i] == null)
                    continue;

                IGH_GeometricGoo shape = geometries[i].DuplicateGeometry();

                shape.Transform(Transform.Translation(0, 0, offset));

                if (shape is Mesh || shape is GH_Mesh)
                {
                    //inMeshes.Add(GH_Convert.ToGeometryBase(shape) as Mesh);
                    myGrids.Add(new Grid(GH_Convert.ToGeometryBase(shape) as Mesh, useCenters: useCenters));
                }
                else if (shape is Brep || shape is GH_Brep)
                {
                    //myGrids.Add(new Grid(GH_Convert.ToGeometryBase(shape) as Brep, gridSize, useCenters: useCenters));
                    inBreps.Add(GH_Convert.ToGeometryBase(shape) as Brep);
                }
                else if (shape is Surface || shape is GH_Surface)
                {
                    //myGrids.Add(new Grid(GH_Convert.ToGeometryBase(shape) as Brep, gridSize, useCenters: useCenters));
                    inBreps.Add(GH_Convert.ToGeometryBase(shape) as Brep);
                }
                else if (shape is Curve || shape is GH_Curve)
                {
                    //myGrids.Add(new Grid(GH_Convert.ToGeometryBase(shape) as Curve, gridSize, useCenters: useCenters));
                    inCrvs.Add(GH_Convert.ToGeometryBase(shape) as Curve);
                }
                else
                {
                    myGrids.Add(null);
                    meshes.Add(null);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "error on an input grid");
                }


            }

            List<Brep> breps = InputGeometryHelper.CurvesToOffsetBreps(inCrvs, edgeOffset) ?? new List<Brep>();

            breps.AddRange(InputGeometryHelper.BrepsToOffsetBreps(inBreps, edgeOffset));


            for (int i = 0; i < breps.Count; i++)
            {
                myGrids.Add(new Grid(breps[i], gridSize, useCenters: useCenters, goLarge));
            }

            for (int i = 0; i < myGrids.Count; i++)
            {
                if (myGrids[i] != null)
                {
                    if (myGrids[i].SimMesh != null)
                        meshes.Add(new GH_Mesh(myGrids[i].SimMesh));
                    GH_Path p = new GH_Path(i);
                    Vector3d oneNormal = myGrids[i].SimMesh.FaceNormals[0];
                    Vector3d[] _normals = new Vector3d[myGrids[i].SimPoints.Count];
                    _normals.Populate(oneNormal);
                    normals.AddRange(_normals.Select(v => new GH_Vector(v)));
                    centers.AddRange(myGrids[i].SimPoints.Select(pt => new GH_Point(pt)));
                }
                
            }


            DA.SetDataList(0, myGrids);
            DA.SetDataList(1, meshes);
            DA.SetDataList(2, centers);
            DA.SetDataList(3, normals);
            //DA.SetData(3, msg);

        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("fcbeb1dc-b545-4322-b280-12fec6a574c7"); }
        }
    }
}
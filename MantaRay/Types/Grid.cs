using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.Helpers;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Rhino.RhinoApp;


namespace MantaRay.Types
{
    /// <summary>
    /// Original (C) Henning Larsen Architects 2019
    /// GPL License
    /// Author: Mathias Sønderskov
    /// https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public
    /// </summary>
    public class Grid
    {

        public int ID { get; set; } = 0;
        public double Area { get; set; }
        public Mesh SimMesh { get; set; }
        public List<Point3d> SimPoints { get; set; } = new List<Point3d>();
        public List<Plane> Planes { get => planes; set => planes = value; }
        List<Plane> planes = new List<Plane>();
        public List<double> FaceAreas { get; set; } = new List<double>();
        internal List<GridResults> Resultss { get; set; }
        public double GridDist { get; set; }

        public string Name { get; set; } = "";

        public bool UseCenters { get; }

        public string GetNames()
        {
            return "Available Names: " + string.Join(", ", Resultss.Select(p => p.Name));
        }

        public override string ToString()
        {
            return $"Grid \"{Name}\" ({SimPoints.Count} points)";
        }

        public Grid()
        {

        }

        public Grid(Brep srf, double gridDist, bool useCenters = false, bool goLarge = false)
        {

            // https://discourse.mcneel.com/t/mesh-fill-holes-best-strategy/71850/14    Strategies to find inner/outer holes for clipper offset TODO.
            var bb = srf.GetBoundingBox(false);
            //Rhino.RhinoApp.WriteLine($"sqrt {Math.Sqrt(bb.Area)} and dist {gridDist}..  divided = {Math.Sqrt(bb.Area) / gridDist}");
            if (bb.Area / (gridDist * gridDist) > 60000 && goLarge)
                throw new Exception("too many points? You can set GoLarge to true");
            SimMesh = GeneratePoints(srf, gridDist, out List<Point3d> pts, out planes, useCenters);


            SimMesh.Normals.ComputeNormals();
            if (SimMesh.FaceNormals[0].Z < 0)
                SimMesh.Flip(true, true, true);

            SimPoints = pts;
            GridDist = gridDist;

            UseCenters = useCenters;
            UpdateAreas();
        }

        public Grid(Mesh mesh, bool useCenters = false)
        {
            mesh.Normals.ComputeNormals();
            if (mesh.FaceNormals[0].Z < 0)
                mesh.Flip(true, true, true);

            SimMesh = GeneratePoints(mesh, out List<Point3d> pts, out planes, useCenters: useCenters);

            SimPoints = pts;
            //SimMesh.Normals.ComputeNormals();

            UseCenters = useCenters;
            UpdateAreas();
        }

        public Grid(Curve curve, double gridDist, bool useCenters = false, bool goLarge = false)
        {


            var srfs = Brep.CreatePlanarBreps(curve, 0.001.FromMeter()); // mesh is faster, but we do breps to automatically sort edge vs hole
            var bb = srfs[0].GetBoundingBox(false);
            //Rhino.RhinoApp.WriteLine($"sqrt {Math.Sqrt(bb.Area)} and dist {gridDist}..  divided = {Math.Sqrt(bb.Area) / gridDist}");
            if (bb.Area / (gridDist * gridDist) > 60000 && goLarge)
                throw new Exception("too many points? You can set GoLarge to true");

            //var surface = NurbsSurface.CreateExtrusion(polyline.ToNurbsCurve(), new Rhino.Geometry.Vector3d(99, 0, 0));

            SimMesh = GeneratePoints(srfs[0], gridDist, out List<Point3d> pts, out planes, useCenters);
            SimMesh.Normals.ComputeNormals();

            if (SimMesh.FaceNormals[0].Z < 0)
                SimMesh.Flip(true, true, true);

            SimPoints = pts;
            GridDist = gridDist;

            UseCenters = useCenters;
            UpdateAreas();
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="mesh"></param>
        ///// <param name="values"></param>
        ///// <param name="min"></param>
        ///// <param name="max"></param>
        ///// <param name="colors">Colors in the gradient</param>
        ///// <param name="colorPlacements">double [0...1] per color in the gradient.</param>
        ///// <returns></returns>
        //public static void ReColorMesh(ref Mesh mesh, List<double> values, double min, double max, List<Color> colors = null)
        //{

        //    //var watch = System.Diagnostics.Stopwatch.StartNew();

        //    //Rhino.RhinoApp.WriteLine($"Grid.cs values.count: {values.Count}, colors.count: {colors.Count}");

        //    List<Color> colorList = new List<Color>();

        //    Grasshopper.GUI.Gradient.GH_Gradient gradient = new Grasshopper.GUI.Gradient.GH_Gradient();

        //    if (colors == null || colors.Count == 0)
        //    {
        //        colors = new List<Color>
        //        {
        //            Color.Blue,
        //            Color.Yellow,
        //            Color.Red
        //        };
        //    }

        //    List<double> colorPlacements = new List<double>();
        //    for (int i = 0; i < colors.Count; i++)
        //        colorPlacements.Add(i / (double)(colors.Count - 1));

        //    for (int i = 0; i < colors.Count; i++)
        //        gradient.AddGrip(colorPlacements[i], colors[i]);

        //    for (int i = 0; i < values.Count; i++)
        //    {
        //        double value = (values[i] - min) / (max - min);
        //        Color col = gradient.ColourAt(value);
        //        colorList.Add(col);

        //    }

        //    ReColorMesh(ref mesh, colorList);

        //}




        public static Mesh GeneratePoints(Mesh analysisMesh, out List<Point3d> analysisPT, out List<Plane> planes, bool useCenters = false)
        {
            double verticalOffset = 0.5.FromMeter();

            analysisPT = new List<Point3d>();
            planes = new List<Plane>();

            // UNSURE IF SIMPT SHOULD BE VERTICES OR CENTERS OF FACES! #TODO  ALSO CHECK IF MESH NEEDS TO BE WELD...
            //foreach (Point3d pt in analysisMesh.Vertices)
            //{
            //    analysisPT.Add(pt);
            //}

            if (useCenters)
            {
                for (int i = 0; i < analysisMesh.Faces.Count; i++)
                {
                    Point3d center = analysisMesh.Faces.GetFaceCenter(i);
                    analysisPT.Add(new Point3d(center.X, center.Y, center.Z + verticalOffset));
                    planes.Add(new Plane(analysisPT[analysisPT.Count - 1], analysisMesh.FaceNormals[i]));
                }


                Mesh mesh = new Mesh();
                for (int i = 0; i < analysisMesh.Faces.Count; i++)
                {
                    Mesh msh = new Mesh();
                    List<Point3d> ptList = new List<Point3d>();
                    MeshFace face = analysisMesh.Faces[i];

                    ptList.Add(new Point3d(analysisMesh.Vertices[face.A]));
                    ptList.Add(new Point3d(analysisMesh.Vertices[face.B]));
                    ptList.Add(new Point3d(analysisMesh.Vertices[face.C]));
                    if (face.IsQuad) ptList.Add(new Point3d(analysisMesh.Vertices[face.D]));

                    MeshFace fc;

                    if (face.IsQuad)
                        fc = new MeshFace(0, 1, 2, 3);
                    else
                        fc = new MeshFace(0, 1, 2);

                    msh.Vertices.AddVertices(ptList.ToArray());
                    msh.Faces.AddFace(fc);

                    mesh.Append(msh);

                }

                return mesh;

            }
            else
            {
                for (int i = 0; i < analysisMesh.Vertices.Count; i++)
                {
                    //Point3d center = analysisMesh.Faces.GetFaceCenter(i);
                    analysisPT.Add(analysisMesh.Vertices[i]);
                }

                return analysisMesh;
            }


        }

        private static Mesh GeneratePoints(Brep analysisSrf, double analysisgriddist, out List<Point3d> analysisPT, out List<Plane> planes, bool useCenters = false)
        {

            Mesh mesh = new Mesh();

            double verticalOffset = 0.5.FromMeter();
            analysisPT = new List<Point3d>();
            planes = new List<Plane>();

            MeshingParameters meshpar = MeshingParameters.Default;
            meshpar.MaximumEdgeLength = analysisgriddist;
            meshpar.MinimumEdgeLength = analysisgriddist;

            Mesh[] analysisMesh = Mesh.CreateFromBrep(analysisSrf, meshpar);

            analysisMesh[0].Normals.ComputeNormals();

            // UNSURE IF SIMPT SHOULD BE VERTICES OR CENTERS OF FACES! #TODO
            if (useCenters)
            {
                for (int i = 0; i < analysisMesh[0].Faces.Count; i++)
                {
                    Point3d center = analysisMesh[0].Faces.GetFaceCenter(i);

                    analysisPT.Add(new Point3d(center.X, center.Y, center.Z + verticalOffset));
                    planes.Add(new Plane(analysisPT[analysisPT.Count - 1], analysisMesh[0].FaceNormals[i]));
                }
            }
            else
            {

                foreach (Point3d pt in analysisMesh[0].Vertices)
                {
                    analysisPT.Add(pt);
                }
                return analysisMesh[0];
            }

            //MeshVertexList pts = analysisMesh[0].Vertices;

            for (int i = 0; i < analysisMesh[0].Faces.Count; i++)
            {
                Mesh msh = new Mesh();
                List<Point3d> ptList = new List<Point3d>();
                MeshFace face = analysisMesh[0].Faces[i];

                ptList.Add(new Point3d(analysisMesh[0].Vertices[face.A]));
                ptList.Add(new Point3d(analysisMesh[0].Vertices[face.B]));
                ptList.Add(new Point3d(analysisMesh[0].Vertices[face.C]));
                if (face.IsQuad) ptList.Add(new Point3d(analysisMesh[0].Vertices[face.D]));

                MeshFace fc;

                if (face.IsQuad)
                    fc = new MeshFace(0, 1, 2, 3);
                else
                    fc = new MeshFace(0, 1, 2);

                msh.Vertices.AddVertices(ptList.ToArray());
                msh.Faces.AddFace(fc);

                mesh.Append(msh);

            }

            return mesh;
        }


        //public static void ReColorMesh(ref Mesh mesh, IEnumerable<Color> colors)
        //{

        //    //var watch = System.Diagnostics.Stopwatch.StartNew();

        //    if (colors.ToList().Count != mesh.Faces.Count)
        //        throw new ArgumentOutOfRangeException("Color Count != Mesh face Count", $"too many/few colors in the list. (Grid.ReColorMesh)");

        //    List<Color> colorList = new List<Color>();
        //    var cols = colors.ToList();

        //    // SLOW PART!

        //    for (int i = 0; i < mesh.Faces.Count; i++)
        //    {
        //        colorList.Add(cols[i]);
        //        colorList.Add(cols[i]);
        //        colorList.Add(cols[i]);
        //        if (mesh.Faces[i].IsQuad)
        //            colorList.Add(cols[i]);
        //    }

        //    //watch = System.Diagnostics.Stopwatch.StartNew();

        //    mesh.VertexColors.SetColors(colorList.ToArray());

        //    //watch.Stop();
        //}

        public Mesh GetColoredMesh(IEnumerable<Color> colors)
        {
            Mesh mesh = SimMesh.DuplicateMesh();
            //var watch = System.Diagnostics.Stopwatch.StartNew();

            List<Color> colorList = new List<Color>();
            var cols = colors.ToList();

            // SLOW PART!
            if (UseCenters)
            {
                if (cols.Count != mesh.Faces.Count)
                    throw new ArgumentOutOfRangeException("Color Count != Mesh face Count", $"too many/few colors in the list. color count = {cols.Count}, faces = {mesh.Faces.Count} (Grid.GetColoredMesh)");

                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    colorList.Add(cols[i]);
                    colorList.Add(cols[i]);
                    colorList.Add(cols[i]);
                    if (mesh.Faces[i].IsQuad)
                        colorList.Add(cols[i]);
                }
            }
            else
            {

                if (cols.Count != mesh.Vertices.Count)
                    throw new ArgumentOutOfRangeException("Color Count != Mesh vertice Count", $"too many/few colors in the list. color count = {cols.Count}, vertices = {mesh.Vertices.Count} (Grid.GetColoredMesh)");
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    colorList.Add(cols[i]);
                }
            }

            //watch = System.Diagnostics.Stopwatch.StartNew();

            mesh.VertexColors.SetColors(colorList.ToArray());

            return mesh;

            //watch.Stop();
        }

        private void UpdateAreas()
        {

            FaceAreas = new List<double>();

            for (int i = 0; i < SimMesh.Faces.Count; i++)
            {
                FaceAreas.Add(MeshFaceArea(SimMesh, i));
            }

            Area = FaceAreas.Sum();
        }

        // http://james-ramsden.com/area-of-a-mesh-face-in-c-in-grasshopper/
        /// <summary>
        /// Get area of a mesh
        /// </summary>
        /// /// <param name="m">Mesh</param>
        /// <param name="meshfaceindex">Mesh index</param>
        /// <returns></returns>
        internal double MeshFaceArea(Mesh m, int meshfaceindex)
        {

            //get points into a nice, concise format
            Point3d[] pts = new Point3d[4];
            pts[0] = m.Vertices[m.Faces[meshfaceindex].A];
            pts[1] = m.Vertices[m.Faces[meshfaceindex].B];
            pts[2] = m.Vertices[m.Faces[meshfaceindex].C];
            if (m.Faces[meshfaceindex].IsQuad) pts[3] = m.Vertices[m.Faces[meshfaceindex].D];

            //calculate areas of triangles
            double a = pts[0].DistanceTo(pts[1]);
            double b = pts[1].DistanceTo(pts[2]);
            double c = pts[2].DistanceTo(pts[0]);
            double p = 0.5 * (a + b + c);
            double area = Math.Sqrt(p * (p - a) * (p - b) * (p - c));

            //if quad, calc area of second triangle
            //double area2 = 0;
            if (m.Faces[meshfaceindex].IsQuad)
            {
                a = pts[0].DistanceTo(pts[2]);
                b = pts[2].DistanceTo(pts[3]);
                c = pts[3].DistanceTo(pts[0]);
                p = 0.5 * (a + b + c);
                area += Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }

            return area;
        }

        public class GridResults
        {
            public string Name { get; set; }
            public double[] Results { get; set; }
            internal GridResults(string name, IEnumerable<double> results)
            {
                Name = name;
                Results = results.ToArray();
            }

        }


    }
}

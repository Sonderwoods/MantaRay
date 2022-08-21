using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer
{


    public class RaPolygon : RadianceGeometry
    {

        public Mesh Mesh { get; set; }


        protected List<Mesh> meshes = new List<Mesh>(64);

        public RaPolygon(string[] data) : base(data)
        {
            int skip = 6;
            IEnumerable<string> dataNoHeader = data.Skip(skip); // skip header

            int i = 0;
            double d1 = -1;
            double d2 = -1;
            int dataCount = data.Length - skip;

            if (dataCount == 9 || dataCount == 12)
            {
                Mesh = new Mesh();

                foreach (var item in dataNoHeader)
                {
                    switch (i++ % 3)
                    {
                        case 0:
                            d1 = double.Parse(item, CultureInfo.InvariantCulture);
                            break;
                        case 1:
                            d2 = double.Parse(item, CultureInfo.InvariantCulture);
                            break;
                        case 2:
                            Mesh.Vertices.Add(new Point3d(d1, d2, double.Parse(item, CultureInfo.InvariantCulture)));
                            break;

                    }

                }

                if (dataCount == 12)
                {
                    Mesh.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    Mesh.Faces.AddFace(0, 1, 2);
                }

            }
            else if (dataCount > 12) // Large polygon. more heavy also as it involves a step through BREPs.
            {
                List<Point3d> ptList2 = new List<Point3d>();

                foreach (var item in dataNoHeader)
                {
                    switch (i++ % 3)
                    {
                        case 0:
                            d1 = double.Parse(item, CultureInfo.InvariantCulture);
                            break;
                        case 1:
                            d2 = double.Parse(item, CultureInfo.InvariantCulture);
                            break;
                        case 2:
                            ptList2.Add(new Point3d(d1, d2, double.Parse(item, CultureInfo.InvariantCulture)));
                            break;

                    }


                }
                
                if (ptList2[0] != ptList2[ptList2.Count - 1])
                {
                    ptList2.Add(ptList2[0]);
                }
                // LBT STYLE
                //var nurbsCurve = new Polyline(ptList2).ToNurbsCurve();
                //var segs = nurbsCurve.DuplicateSegments();
                //var borderLines = segs.Where(s => !IsCurveDup(s, segs));
                //var border = Curve.JoinCurves(borderLines);
                //var breps = Brep.CreatePlanarBreps(border, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                //var m = Mesh.CreateFromBrep(breps[0], MeshingParameters.Default);
                //Mesh = m[0];


                var segs = new Polyline(ptList2).ToNurbsCurve().DuplicateSegments();

                var border = Curve.JoinCurves(segs.AsParallel().AsOrdered().Where(s => !IsCurveDup(s, segs)));

                var breps = Brep.CreatePlanarBreps(border, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                var brep = breps?[0];

                if (brep != null)
                    Mesh = Mesh.CreateFromBrep(brep, MeshingParameters.Default)[0];
                else
                {

                    throw new PolygonException($"Could not create polygon from:\n{String.Join(" ", data)}");
                }



            }
            else
            {
                throw new SyntaxException("Not really the format of a face. Vertices were < 12 but not 9.");
            }




        }


        public void AddTempMesh(Mesh mesh, bool update = false)
        {
            meshes.Add(mesh);
            if (update)
                UpdateMesh();
        }

        public void UpdateMesh()
        {
            if (meshes.Count == 0)
                return;
            if (Mesh == null)
                Mesh = new Mesh();
            Mesh.Append(meshes);
            Mesh.Faces.CullDegenerateFaces();
            meshes.Clear();
        }

        public static bool IsCurveDup(Curve crv, Curve[] curves)
        {
            List<Point3d> pts = new List<Point3d>(4) { crv.PointAtStart, crv.PointAtEnd };
            int count = 0;
            foreach (var c in curves)
            {
                if (pts.Contains(c.PointAtStart) && pts.Contains(c.PointAtEnd))
                    count++;
            }

            return count > 1;
        }



        //public void DrawObject(IGH_PreviewArgs args, double alpha = 1.0)
        //{
        //    double oldTrans = Material.Transparency;
        //    Material.Transparency = 1.0 - alpha;
        //    args.Display.DrawMeshShaded(Mesh, Material); //works with twosided
        //    Material.Transparency = oldTrans;
        //}

        public override void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material, double? transparency = null)
        {
            if (transparency != null)
            {
                double oldTrans = material.Transparency;
                material.Transparency = transparency.Value;
                args.Display.DrawMeshShaded(Mesh, material);
                material.Transparency = oldTrans;
            }
            else
            {
                args.Display.DrawMeshShaded(Mesh, material);

            }

        }

        public override BoundingBox? GetBoundingBox()
        {
            return Mesh.GetBoundingBox(false);
        }



        public override void DrawWires(IGH_PreviewArgs args, int thickness = 1)
        {
            args.Display.DrawMeshWires(Mesh, System.Drawing.Color.Black, 1);
        }

        public override IEnumerable<GeometryBase> GetGeometry()
        {
            yield return Mesh;
        }

        [Serializable]
        internal class PolygonException : Exception
        {
            public PolygonException(string message) : base(message) { }
        }

        [Serializable]
        internal class SyntaxException : Exception
        {
            public SyntaxException(string message) : base(message) { }
        }


    }
}

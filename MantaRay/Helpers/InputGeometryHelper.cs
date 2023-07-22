using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.ClipperLib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MantaRay.Helpers
{
    /// <summary>
    /// Original (C) Henning Larsen Architects 2019
    /// GPL License
    /// Author: Mathias Sønderskov
    /// https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public
    /// </summary>
    public static class InputGeometryHelper
    {

        /// <summary>
        /// Using clipper
        /// </summary>
        /// <param name="breps"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public static List<Brep> BrepsToOffsetBreps(List<Brep> breps, double dist)
        {
            List<Brep> brepsOut = new List<Brep>();
            //List<Polyline> polyOut2 = new List<Polyline>();

            double tolerance = 0.01.FromMeter();
            var openType = new List<Polyline3D.OpenFilletType> { Polyline3D.OpenFilletType.Butt };
            List<Polyline3D.ClosedFilletType> closedType = new List<Polyline3D.ClosedFilletType>() { Polyline3D.ClosedFilletType.Miter };
            double miter = 0.5.FromMeter();


            for (int i = 0; i < breps.Count; i++)
            {

                List<Curve> allEdges = new List<Curve>();
                if (dist > 0)
                {

                    Curve[] innerEdges = Curve.JoinCurves(breps[i].DuplicateNakedEdgeCurves(false, true));
                    Curve[] outerEdges = Curve.JoinCurves(breps[i].DuplicateNakedEdgeCurves(true, false));

                    outerEdges[0].TryGetPlane(out Plane pln, tolerance);

                    if (pln.ZAxis.Z < 0)
                        pln.Flip();

                    List<Polyline> polylineInner = new List<Polyline>();




                    for (int j = 0; j < innerEdges.Length; j++)
                    {
                        innerEdges[j].TryGetPolyline(out Polyline polyEdge);
                        if (polyEdge != null)
                        {

                            var crv = polyEdge.ToNurbsCurve();
                            CurveOrientation dir = crv.ClosedCurveOrientation(pln);
                            if (dir == CurveOrientation.Clockwise)
                            {
                                polyEdge.Reverse();
                            }
                            polylineInner.Add(polyEdge);
                            //polyOut.Add(polyEdge);

                        }
                    }

                    List<Polyline> polylineOuter = new List<Polyline>();
                    for (int j = 0; j < outerEdges.Length; j++)
                    {
                        outerEdges[j].TryGetPolyline(out Polyline polyEdge);
                        if (polyEdge != null)
                        {
                            var crv = polyEdge.ToNurbsCurve();
                            CurveOrientation dir = crv.ClosedCurveOrientation(pln);
                            if (dir == CurveOrientation.Clockwise)
                            {
                                polyEdge.Reverse();
                            }
                            polylineOuter.Add(polyEdge);
                        }
                    }

                    Polyline3D.Offset(polylineInner, openType, closedType, pln, tolerance, new List<double> { dist }, miter, arcTolerance: 0.25, outContour: out List<List<Polyline>> brepHoles, out _, EndType.etClosedLine);
                    Polyline3D.Offset(polylineOuter, openType, closedType, pln, tolerance, new List<double> { dist }, miter, arcTolerance: 0.25, out _, outHoles: out List<List<Polyline>> brepEdges, EndType.etClosedLine);

                    var holesCrvs = brepHoles.SelectMany(p => p).Select(p => (Curve)p.ToPolylineCurve()).ToList();
                    var edgeCrvs = brepEdges.SelectMany(p => p).Select(p => (Curve)p.ToPolylineCurve()).ToList();

                    var result = Polyline3D.Boolean(ClipType.ctDifference, brepEdges[0], brepHoles[0], pln, tolerance, true);//var polylinesB = Polyline3D.ConvertCurvesToPolyline(curvesB).ToList();

                    allEdges.AddRange(result.Select(p => (Curve)p.ToPolylineCurve()).ToList());

                    //polyOut2.AddRange(brepHoles.SelectMany(p => p));
                    //polyOut2.AddRange(brepEdges.SelectMany(p => p));

                    brepsOut.AddRange(UpwardsPointingBrepsFromCurves(allEdges));

                }
                else
                {
                    brepsOut.Add(breps[i].TurnUp());
                }



            }
            return brepsOut;


        }


        public static List<Brep> CurvesToOffsetBreps(List<Curve> curves, double dist)
        {

            double tolerance = 0.01.FromMeter();
            var openType = new List<Polyline3D.OpenFilletType> { Polyline3D.OpenFilletType.Butt };
            List<Polyline3D.ClosedFilletType> closedType = new List<Polyline3D.ClosedFilletType>() { Polyline3D.ClosedFilletType.Miter };
            double miter = 0.5.FromMeter();


            List<List<Polyline>> holes = new List<List<Polyline>>();
            List<Brep> brepsOut = new List<Brep>();

            if (curves.Count == 0)
            {
                return null;
            }
            List<Polyline> inPolylines = Polyline3D.ConvertCurvesToPolyline(curves).ToList();
            var pln = inPolylines.First().FitPlane();

            if (dist == 0)
            {
                holes.Add(inPolylines);
                List<Curve> holesPoly = holes.SelectMany(p => p).Select(p => (Curve)p.ToPolylineCurve()).ToList();
                brepsOut.AddRange(UpwardsPointingBrepsFromCurves(holesPoly));
                //outside.Add(new List<Polyline>() { null });
            }
            else
            {
                // endtypes: http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/EndType.htm
                Polyline3D.Offset(inPolylines, openType, closedType, pln, tolerance, new List<double> { dist }, miter, arcTolerance: 0.25, out _, out holes, EndType.etClosedLine);

                List<Curve> holesPoly = holes.SelectMany(p => p).Select(p => (Curve)p.ToPolylineCurve()).ToList();
                brepsOut.AddRange(UpwardsPointingBrepsFromCurves(holesPoly));
            }

            return brepsOut;
        }


        public static Brep[] UpwardsPointingBrepsFromCurves(IEnumerable<Curve> curves)
        {



            Brep[] srfs = Brep.CreatePlanarBreps(curves, 0.001.FromMeter());

            if (srfs == null)
                return new Brep[0];

            for (int j = 0; j < srfs.Length; j++)
            {
                srfs[j].TurnUp();
            }

            return srfs;
        }

        public static Brep TurnUp(this Brep brep)
        {
            var props = AreaMassProperties.Compute(brep, false, true, false, false);
            Point3d center = props.Centroid;
            BrepFace face = brep.Faces[0];


            face.ClosestPoint(center, out double u, out double v);
            Vector3d normal = face.NormalAt(u, v);
            normal.Unitize();
            if (normal.Z < 0)
                brep.Flip();
            return brep;
        }

        public static Mesh ParseToJoinedMesh(List<GH_Brep> breps)
        {
            List<IGH_GeometricGoo> geometricGoos = new List<IGH_GeometricGoo>();

            for (int i = 0; i < breps.Count; i++)
            {
                geometricGoos.Add(GH_Convert.ToGeometricGoo(breps[i]));
            }

            return ParseToJoinedMesh(geometricGoos, out _);
        }


        public static Mesh ParseToJoinedMesh(List<Brep> breps)
        {
            List<IGH_GeometricGoo> geometricGoos = new List<IGH_GeometricGoo>();

            for (int i = 0; i < breps.Count; i++)
            {
                geometricGoos.Add(GH_Convert.ToGeometricGoo(new GH_Brep(breps[i])));
            }

            return ParseToJoinedMesh(geometricGoos, out _);
        }

        /// <summary>
        /// input a List/IGH_GeometricGoo/
        /// List/IGH_GeometricGoo/ shapes = new List/IGH_GeometricGoo/();
        /// DA.GetDataList/IGH_GeometricGoo/(2, shapes);
        /// </summary>
        /// <param name="geometricGoos">List IGH_GeometricGoo  shapes, can be Brep and Mesh</param>
        /// <returns>joinedMesh</returns>
        public static Mesh ParseToJoinedMesh(List<IGH_GeometricGoo> geometricGoos)
        {

            return ParseToJoinedMesh(geometricGoos, out _);
        }


        /// <summary>
        /// input a List/IGH_GeometricGoo/
        /// List/IGH_GeometricGoo/ shapes = new List/IGH_GeometricGoo/();
        /// DA.GetDataList/IGH_GeometricGoo/(2, shapes);
        /// </summary>
        /// <param name="geometricGoos">List IGH_GeometricGoo  shapes, can be Brep and Mesh</param>
        /// <returns>joinedMesh</returns>
        /// 
        public static Mesh ParseToJoinedMesh(List<IGH_GeometricGoo> geometricGoos, out BoundingBox boundingBox, bool parallel = true)
        {
            boundingBox = new BoundingBox();

            if (geometricGoos.Count == 0) return null;

            Mesh[] meshes = new Mesh[geometricGoos.Count];

            Mesh joinedMesh = new Mesh();

            if (parallel)
            {

                Parallel.For(0, geometricGoos.Count, delegate (int i)
                {
                    IGH_GeometricGoo shape = geometricGoos[i];

                    if (shape is Mesh || shape is GH_Mesh)
                    {
                        var geobase = GH_Convert.ToGeometryBase(shape);
                        meshes[i] = geobase as Mesh;

                    }
                    else if (shape is Brep || shape is GH_Brep)
                    {
                        var geobase = GH_Convert.ToGeometryBase(shape);
                        Brep brep = geobase as Brep;
                        MeshingParameters mp = MeshingParameters.Default;

                        Mesh[] meshParts = Mesh.CreateFromBrep(brep, mp);

                        meshes[i] = meshParts[0];

                        for (int j = 1; j < meshParts.Length; j++)
                        {
                            meshes[i].Append(meshParts[j]);
                        }

                    }

                });
            }
            else
            {
                for (int i = 0; i < geometricGoos.Count; i++)
                {

                    IGH_GeometricGoo shape = geometricGoos[i];

                    if (shape is Mesh || shape is GH_Mesh)
                    {
                        var geobase = GH_Convert.ToGeometryBase(shape);
                        meshes[i] = geobase as Mesh;

                    }
                    else if (shape is Brep || shape is GH_Brep)
                    {
                        var geobase = GH_Convert.ToGeometryBase(shape);
                        Brep brep = geobase as Brep;
                        MeshingParameters mp = MeshingParameters.Default;

                        Mesh[] meshParts = Mesh.CreateFromBrep(brep, mp);

                        meshes[i] = meshParts[0];

                        for (int j = 1; j < meshParts.Length; j++)
                        {
                            meshes[i].Append(meshParts[j]);
                        }

                    }

                }

            }

            foreach (var mesh in meshes)
            {
                if (mesh != null)
                    joinedMesh.Append(mesh);
            }


            CleanMesh(joinedMesh);
            boundingBox = joinedMesh.GetBoundingBox(true);

            return joinedMesh;

        }

        /// <summary>
        /// Parses all geometries and adds to the linked lists. This does not clear the lists.
        /// </summary>
        /// <param name="geometricGoos"></param>
        /// <param name="breps"></param>
        /// <param name="meshes"></param>
        /// <param name="curves"></param>
        public static void ParseAll(IList<IGH_GeometricGoo> geometricGoos, List<Brep> breps = null, List<Mesh> meshes = null, List<Curve> curves = null)
        {

            for (int i = 0; i < geometricGoos.Count; i++)
            {
                IGH_GeometricGoo shape = geometricGoos[i];

                if (shape is Mesh || shape is GH_Mesh)
                {
                    if (meshes != null)
                        meshes.Add(GH_Convert.ToGeometryBase(shape) as Mesh);
                }
                if (shape is Brep || shape is GH_Brep)
                {
                    if (breps != null)
                        breps.Add(GH_Convert.ToGeometryBase(shape) as Brep);
                }
                if (shape is Curve || shape is GH_Curve)
                {
                    if (curves != null)
                        curves.Add(GH_Convert.ToGeometryBase(shape) as Curve);
                }

            }

        }


        /// <summary>
        /// Cleans a mesh (weld, rebuild normals etc)
        /// </summary>
        /// <param name="joinedMesh"></param>
        public static void CleanMesh(Mesh joinedMesh)
        {
            //joinedMesh.FaceNormals.ComputeFaceNormals(); // only needed if we we dont do rebuild later.
            //joinedMesh.Vertices.CombineIdentical(true, true);
            joinedMesh.Weld(Math.PI / 2.5);
            joinedMesh.Faces.CullDegenerateFaces();
            //joinedMesh.UnifyNormals(); // will make it blurry
            joinedMesh.RebuildNormals();
            joinedMesh.Compact();

        }

    }
}

using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Helpers
{
    public class RTreeHelper
    {
        public static double[] FindClosestWeightedValues(Grid grid, IList<double> results, bool parallel = false)
        {
            if (grid.UseCenters == false)
                throw new Exception("should only be called on nonCenter grids");

            List<Point3d> searchPoints = grid.SimMesh.Vertices.ToPoint3dArray().ToList();
            double CollisionDistance = grid.GridDist * 2.5;
            var targetMesh = grid.SimMesh;
            RTree rTree = new RTree();

            List<List<int>> potentialTargetsPerPoint = new List<List<int>>();
            int[] closestTargetPerPoint = new int[searchPoints.Count].Populate(-1);
            int[][] closestTargetsPerPoint = new int[searchPoints.Count][];

            double[] finalResults = new double[searchPoints.Count];

            List<List<Point3d>> roomVertices = new List<List<Point3d>>();

            for (int i = 0; i < searchPoints.Count; i++)
            {
                potentialTargetsPerPoint.Add(new List<int>());
                closestTargetsPerPoint[i] = new int[targetMesh.Faces[i].IsQuad ? 4 : 3];
            }

            for (int i = 0; i < targetMesh.Vertices.Count; i++)
                rTree.Insert(targetMesh.Vertices[i], i);

            for (int i = 0; i < searchPoints.Count; i++)
            {
                rTree.Search(
                    new Sphere(searchPoints[i], CollisionDistance),
                    (sender, args) => { potentialTargetsPerPoint[i].Add(args.Id); });
            }

            for (int i = 0; i < potentialTargetsPerPoint.Count; i++)
                potentialTargetsPerPoint[i] = potentialTargetsPerPoint[i].Distinct().ToList();

            if (!parallel)
            {
                for (int i = 0; i < searchPoints.Count; i++)
                {

                    if (potentialTargetsPerPoint.Count > 0)
                    {

                        var list = new[]
                        {
                            new {dist = double.MaxValue, result = 0.0 },
                            new {dist = double.MaxValue, result = 0.0 },
                            new {dist = double.MaxValue, result = 0.0 }
                        }.ToList();
                        if (closestTargetsPerPoint[i].Length == 4) // isquad
                            list.Add(new { dist = double.MaxValue, result = 0.0 });

                        for (int j = 0; j < potentialTargetsPerPoint[i].Count; j++)
                        {

                            var targetPoint = targetMesh.Vertices[potentialTargetsPerPoint[i][j]];

                            double distance = searchPoints[i].DistanceTo(targetPoint);

                            if (distance < list[list.Count - 1].dist)
                            {

                                closestTargetPerPoint[i] = potentialTargetsPerPoint[i][j];

                                list[closestTargetsPerPoint[i].Length] = new { dist = distance, result = results[closestTargetPerPoint[i]] };

                                list.OrderBy(l => l.dist);

                            }
                        }

                        finalResults[i] = list.Select(l => l.result).Sum() / closestTargetsPerPoint[i].Length;

                    }

                }

            }
            else
            {
                Parallel.For(0, searchPoints.Count, i =>
                {
                    if (potentialTargetsPerPoint.Count > 0)
                    {

                        var list = new[]
                        {
                            new {dist = double.MaxValue, result = 0.0 },
                            new {dist = double.MaxValue, result = 0.0 },
                            new {dist = double.MaxValue, result = 0.0 }
                        }.ToList();
                        if (closestTargetsPerPoint[i].Length == 4) // isquad
                            list.Add(new { dist = double.MaxValue, result = 0.0 });

                        for (int j = 0; j < potentialTargetsPerPoint[i].Count; j++)
                        {

                            var targetPoint = targetMesh.Vertices[potentialTargetsPerPoint[i][j]];

                            double distance = searchPoints[i].DistanceTo(targetPoint);

                            if (distance < list[list.Count - 1].dist)
                            {

                                closestTargetPerPoint[i] = potentialTargetsPerPoint[i][j];

                                list[closestTargetsPerPoint[i].Length] = new { dist = distance, result = results[closestTargetPerPoint[i]] };

                                list.OrderBy(l => l.dist);

                            }
                        }

                        finalResults[i] = list.Select(l => l.result).Sum() / closestTargetsPerPoint[i].Length;

                    }


                });
            }


            return finalResults;

        }
        public static int[] ConnectPointsToBreps(IEnumerable<Point3d> points, List<Brep> breps, double maxDist = 5, double rTreeTolerance = -1, bool parallel = false)
        {
            bool autoRTraceTol = rTreeTolerance <= 0;
            int ptCount = points.Count();
            RTree rTree = new RTree();

            List<List<int>> potentialBrepsPerPoint = new List<List<int>>();

            int[] closestBrepPerPoint = new int[ptCount];
            for (int i = 0; i < closestBrepPerPoint.Length; i++)
            {
                closestBrepPerPoint[i] = -1;
            }

            List<List<Point3d>> brepVertices = new List<List<Point3d>>();



            for (int i = 0; i < ptCount; i++)
            {
                potentialBrepsPerPoint.Add(new List<int>());

            }

            for (int i = 0; i < breps.Count; i++)
            {

                List<Point3d> corners = breps[i].Vertices.Distinct().Select(v => v.Location).ToList();

                if (autoRTraceTol)
                {
                    rTreeTolerance = Math.Max(rTreeTolerance, new BoundingBox(corners).Diagonal.Length);
                }

                brepVertices.Add(corners);

                foreach (var corner in corners)
                {
                    rTree.Insert(corner, i);

                }

            }




            if (!parallel)
            {

                foreach (var item in points.Select((value, i) => new { i, value }))
                {

                    rTree.Search(
                        new Sphere(item.value, rTreeTolerance),
                        (sender, args) => { potentialBrepsPerPoint[item.i].Add(args.Id); });
                }


                for (int i = 0; i < potentialBrepsPerPoint.Count; i++)
                {
                    potentialBrepsPerPoint[i] = potentialBrepsPerPoint[i].Distinct().ToList();

                }

                foreach (var item in points.Select((value, i) => new { i, value }))
                {

                    double dist = maxDist;

                    if (potentialBrepsPerPoint.Count > 0)
                    {

                        for (int j = 0; j < potentialBrepsPerPoint[item.i].Count; j++)
                        {

                            Brep targetBrep = breps[potentialBrepsPerPoint[item.i][j]];

                            targetBrep.ClosestPoint(item.value, out Point3d p, out _, out _, out _, dist, out _);

                            double distance = item.value.DistanceTo(p);

                            if (distance < dist)
                            {
                                closestBrepPerPoint[item.i] = potentialBrepsPerPoint[item.i][j];
                                dist = distance;
                            }
                        }
                    }

                }

            }
            else
            {
                Parallel.ForEach(points, (p, s, i) =>
                {

                    rTree.Search(
                        new Sphere(p, rTreeTolerance),
                        (sender, args) =>
                        {
                            potentialBrepsPerPoint[(int)i].Add(args.Id);
                        });
                });


                Parallel.For(0, potentialBrepsPerPoint.Count, (i) =>
                {
                    potentialBrepsPerPoint[i] = potentialBrepsPerPoint[i].Distinct().ToList();

                });


                Parallel.ForEach(points, (p, s, i) =>
                //Parallel.For(0, _pts.Count, i =>
                {
                    double dist = maxDist;

                    if (potentialBrepsPerPoint.Count > 0)
                    {

                        for (int j = 0; j < potentialBrepsPerPoint[(int)i].Count; j++)
                        {

                            Brep brep = breps[potentialBrepsPerPoint[(int)i][j]];

                            brep.ClosestPoint(p, out Point3d p2, out _, out _, out _, dist, out _);

                            double distance = p.DistanceTo(p2);

                            if (distance < dist)
                            {
                                closestBrepPerPoint[i] = potentialBrepsPerPoint[(int)i][j];
                                dist = distance;
                            }
                        }
                    }


                });
            }


            return closestBrepPerPoint;

        }
    }
}

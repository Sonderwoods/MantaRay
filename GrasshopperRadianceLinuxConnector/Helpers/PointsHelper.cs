//using Grasshopper.Kernel.Geometry;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrasshopperRadianceLinuxConnector
{
    public static class PointsHelper
    {
        public static List<Plane> ReadPtsString(string ptsString)
        {
            List<Plane> planes = new List<Plane>();

            foreach (string line in ptsString.Split('\n').Where(l => !l.StartsWith("#")))
            {
               
                double[] n = line.Replace('\t', ' ').Split(' ')
                    .Where(s => !String.IsNullOrEmpty(s))
                    .Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                if(n.Length == 6)
                {
                planes.Add(new Plane(new Point3d(n[0], n[1], n[2]), new Vector3d(n[3], n[4], n[5])));

                }
            }

            return planes;
        } 
    }
}

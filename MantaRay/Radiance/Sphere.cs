using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Radiance
{

    /// <summary>
    /// For the inverse, check out the <see cref="Bubble"/>
    /// </summary>
    public class Sphere : Polygon
    {

        Rhino.Geometry.Sphere? sphere;

        public Sphere(string[] data, bool flipNormals = false) : base(data)
        {
            double[] dataNoHeader = data.Skip(6).Select(i => double.Parse(i)).ToArray(); // skip header

            if (dataNoHeader.Count() != 4)
            {
                throw new SyntaxException("Wrong number of parameters in the sphere (should be 4) " + data[3]);
            }

            sphere = new Rhino.Geometry.Sphere(
                new Point3d(dataNoHeader[0], dataNoHeader[1], dataNoHeader[2]),
                dataNoHeader[3]);

            Mesh = Mesh.CreateFromSphere(sphere.Value, 16, 16);
            if (flipNormals)
                Mesh.Flip(true, true, true);

        }

        public override void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material, double? transparency = null)
        {
            if (sphere != null)
                args.Display.DrawSphere(sphere.Value, material.Diffuse);
        }

        public override IEnumerable<GeometryBase> GetGeometry()
        {
            yield return Brep.CreateFromSphere(sphere.Value);
        }

        public override BoundingBox? GetBoundingBox()
        {
            BoundingBox b = new BoundingBox(new[] { sphere.Value.Center });
            b.Inflate(sphere.Value.Diameter);
            return b;
        }

        public override void DrawWires(IGH_PreviewArgs args, int thickness = 1)
        {
            args.Display.DrawSphere(sphere.Value, Material.Diffuse);
        }


    }
}

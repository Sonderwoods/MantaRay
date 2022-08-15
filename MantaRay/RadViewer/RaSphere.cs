using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer
{

    /// <summary>
    /// For the inverse, check out the <see cref="RaBubble"/>
    /// </summary>
    public class RaSphere : RaPolygon
    {

        Sphere? sphere;

        public RaSphere(string[] data, bool flipNormals = false) : base(data)
        {
            double[] dataNoHeader = data.Skip(6).Select(i => double.Parse(i)).ToArray(); // skip header

            if (dataNoHeader.Count() != 4)
            {
                throw new SyntaxException("Wrong number of parameters in the sphere (should be 4) " + data[3]);
            }

            sphere = new Sphere(
                new Point3d(dataNoHeader[0], dataNoHeader[1], dataNoHeader[2]),
                dataNoHeader[3]);

            Mesh = Mesh.CreateFromSphere(sphere.Value, 16, 16);
            if (flipNormals)
                Mesh.Flip(true, true, true);

        }

        public override void DrawObject(IGH_PreviewArgs args, double alpha = 1.0)
        {
            if (sphere != null)
                args.Display.DrawSphere(sphere.Value, System.Drawing.Color.Black);
        }


    }
}

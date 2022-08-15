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
    /// A cylinder .. For the inverse check out the <see cref="RaTube"/>
    /// </summary>
    public class RaCylinder : RaPolygon
    {
        public virtual void FlipNormals() { }
        Cylinder? cylinder;

        public RaCylinder(string[] data, bool flipNormals = false) : base(data)
        {
            double[] dataNoHeader = data.Skip(6).Select(i => double.Parse(i)).ToArray(); // skip header



            if (dataNoHeader.Count() != 7)
            {
                throw new SyntaxException("Wrong number of parameters in the cylinder (should be 7) " + data[3]);
            }
            Vector3d dir = new Vector3d(dataNoHeader[3] - dataNoHeader[0], dataNoHeader[4] - dataNoHeader[1], dataNoHeader[5] - dataNoHeader[2]);

            cylinder = new Cylinder(
                new Circle(
                    new Plane(
                        new Point3d(dataNoHeader[0], dataNoHeader[1], dataNoHeader[2]),
                        dir
                        ),
                    dataNoHeader[6]),
                dir.Length);

            Mesh = Mesh.CreateFromCylinder(cylinder.Value, 1, 16, true, true);
            if (flipNormals)
            {
                Mesh.Flip(true, true, true);

            }

        }

        public override void DrawObject(IGH_PreviewArgs args, double alpha = 1.0)
        {
            if (cylinder.HasValue)
            {
                args.Display.DrawCylinder(cylinder.Value, System.Drawing.Color.Black);

            }
        }

    }
}

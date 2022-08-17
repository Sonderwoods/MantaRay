using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
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
        Point3d StartPoint;
        Point3d EndPoint;

        public RaCylinder(string[] data, bool flipNormals = false) : base(data)
        {
            double[] dataNoHeader = data.Skip(6).Select(i => double.Parse(i)).ToArray(); // skip header



            if (dataNoHeader.Count() != 7)
            {
                throw new SyntaxException("Wrong number of parameters in the cylinder (should be 7) " + data[3]);
            }
            //Vector3d dir = new Vector3d(dataNoHeader[3] - dataNoHeader[0], dataNoHeader[4] - dataNoHeader[1], dataNoHeader[5] - dataNoHeader[2]);
            StartPoint = new Point3d(dataNoHeader[0], dataNoHeader[1], dataNoHeader[2]);
            EndPoint = new Point3d(dataNoHeader[3], dataNoHeader[4], dataNoHeader[5]);
            Vector3d dir = EndPoint - StartPoint;

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

        public override void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material)
        {
            if (cylinder.HasValue)
            {
                args.Display.DrawCylinder(cylinder.Value, (material ?? Material).Diffuse);
                args.Display.DrawMeshShaded(Mesh.CreateFromCylinder(cylinder.Value, 20, 20), material ?? Material);
            }
        }

        public override void DrawWires(IGH_PreviewArgs args, int thickness = 1)
        {
            if (cylinder.HasValue)
            {
                args.Display.DrawCylinder(cylinder.Value, Material.Diffuse);

            }
        }

        public override BoundingBox? GetBoundingBox()
        {
            if (cylinder.HasValue)
            {
                BoundingBox b = new BoundingBox(StartPoint, EndPoint);
                b.Inflate(cylinder.Value.Radius);
                return b;

            }
            return null;
        }

        public override IEnumerable<GeometryBase> GetGeometry()
        {
            if (cylinder.HasValue)
            {
                yield return Brep.CreateFromCylinder(cylinder.Value, true, true);
            }
            
        }

    }
}

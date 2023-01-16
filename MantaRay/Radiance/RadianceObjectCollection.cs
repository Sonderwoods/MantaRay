using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.Radiance.HeadsUpDisplay;
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
    /// A collection of radiance objects (typically with the same modifier name)
    /// </summary>
    public class RadianceObjectCollection : RadianceGeometry
    {


        private readonly List<IHasPreview> objects = new List<IHasPreview>();

        public List<IHasPreview> Objects => objects;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName">Could be the shared modifier name</param>
        public RadianceObjectCollection(string groupName)
        {
            Name = groupName;
        }

        public void AddObject(RadianceGeometry obj)
        {
            if (obj is Polygon p && objects.OfType<Polygon>().Any())
            {
                objects.OfType<Polygon>().First().AddTempMesh(p.Mesh);
            }
            else
            {
                objects.Add(obj);
            }
        }

        public void UpdateMesh()
        {
            var item = objects.OfType<Polygon>().FirstOrDefault();
            if (item != null)
            {
                item.UpdateMesh();
            }
        }

        public override void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material, double? transparency = null)
        {
            foreach (var obj in objects)
            {
                obj?.DrawPreview(args, material);
            }
        }

        public override void DrawWires(IGH_PreviewArgs args, int thickness = 1)
        {
            foreach (var obj in objects)
            {
                obj?.DrawWires(args, thickness);
            }
        }

        public override IEnumerable<GeometryBase> GetGeometry()
        {

            foreach (var item in objects)
            {
                foreach (var obj in item.GetGeometry())
                {
                    yield return obj;

                }

            }

        }

        public override BoundingBox? GetBoundingBox()
        {
            var obj = objects.FirstOrDefault();

            BoundingBox? bb = null;

            if (obj == null || !obj.GetBoundingBox().HasValue)
            {
                return null;
                
            }
            else
            {
                bb = obj.GetBoundingBox();
            }
            foreach (var item in objects.Skip(1).Where(i => i.GetBoundingBox().HasValue))
            {
                bb.Value.Union(item.GetBoundingBox().Value);
            }

            return bb;
        }
    }
}

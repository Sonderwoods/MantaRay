using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.RadViewer.HeadsUpDisplay;
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
            if (obj is RaPolygon p && objects.OfType<RaPolygon>().Any())
            {
                objects.OfType<RaPolygon>().First().AddTempMesh(p.Mesh);
            }
            else
            {
                objects.Add(obj);
            }
        }

        public void UpdateMesh()
        {
            var item = objects.OfType<RaPolygon>().FirstOrDefault();
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
    }
}

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

    public abstract class RadianceGeometry : RadianceObject, IHasPreview
    {
        public BoundingBox? BBox { get; set; }
        public Rhino.Display.DisplayMaterial Material { get; set; } = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Gray);

        public RadianceGeometry(string[] data) : base(data)
        {

        }

        public RadianceGeometry()
        {

        }

       



        public virtual BoundingBox? GetBoundingBox()
        {
            return BBox;
        }

        public virtual bool HasPreview() => true;

        public virtual string GetName() => Name;
        public virtual string GetDescription()
        {
            if (Modifier != null && Modifier is RadianceMaterial m)
            {
                return m.MaterialDefinition ?? "No Modifiers found";
            }
             return "No Modifiers found";
        }
        public abstract void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material);
        public abstract void DrawWires(IGH_PreviewArgs args, int thickness = 1);
        public abstract IEnumerable<GeometryBase> GetGeometry();
    }
}

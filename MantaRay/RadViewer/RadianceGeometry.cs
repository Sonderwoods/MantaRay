using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer
{

    public abstract class RadianceGeometry : RadianceObject
    {
        public BoundingBox? BBox { get; set; }
        public Rhino.Display.DisplayMaterial Material { get; set; } = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Gray);

        public RadianceGeometry(string[] data) : base(data)
        {

        }



        public override BoundingBox? GetBoundingBox()
        {
            return BBox;
        }

        public override bool HasPreviewBrep() => true;
        public override bool HasPreviewMesh() => true;

        public abstract void DrawObject(IGH_PreviewArgs args, double alpha = 1.0);
        public abstract void DrawWires(IGH_PreviewArgs args);
    }
}

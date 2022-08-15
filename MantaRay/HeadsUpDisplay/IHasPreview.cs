using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer.HeadsUpDisplay
{
    public interface IHasPreview
    {

        string GetName();
        string GetDescription();
        bool HasPreviewMesh();
        bool HasPreviewBrep();
        void DrawPreviewMesh(IGH_PreviewArgs args, double alpha = 1.0);
        void DrawPreviewBrep(IGH_PreviewArgs args, double alpha = 1.0);
        BoundingBox? GetBoundingBox();
    }
}

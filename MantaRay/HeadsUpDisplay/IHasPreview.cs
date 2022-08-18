using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer.HeadsUpDisplay
{
    /// <summary>
    /// IHasPreview must be used for all HUD items
    /// </summary>
    public interface IHasPreview
    {
        /// <summary>
        /// The name is the displayed title of an object in the HUD browser
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// The description is the mouse over text
        /// </summary>
        /// <returns></returns>
        string GetDescription();

        /// <summary>
        /// A boolean showing if a preview is available upon mouse over
        /// </summary>
        /// <returns></returns>
        bool HasPreview();

        /// <summary>
        /// The Rhino draw method. Can be used for meshes/brep/primitives etc
        /// </summary>
        /// <param name="args"></param>
        /// <param name="alpha">Alpha 0..1</param>
        void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material = null, double? transparency = null);

        /// <summary>
        /// The Rhino wireframe draw method.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="thickness">Wire thickness</param>
        void DrawWires(IGH_PreviewArgs args, int thickness = 1);
        BoundingBox? GetBoundingBox();

        IEnumerable<GeometryBase> GetGeometry(); 
    }
}

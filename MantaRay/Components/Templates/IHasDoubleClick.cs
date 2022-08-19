using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Components.Templates
{
    public interface IHasDoubleClick
    {
        GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e);
    }
}

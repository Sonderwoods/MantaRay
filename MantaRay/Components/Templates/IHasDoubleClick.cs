using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using MantaRay.Components.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Components.Templates
{
    /// <summary>
    /// ENables the OnDoubleClick on your component. Remember that you must set your attributes like this:<br/>
    /// override <see cref="Grasshopper.Kernel.GH_Component.CreateAttributes"/> with:<br/>
    /// m_attributes = new <see cref="MantaRay.Components.Templates.GH_DoubleClickAttributes"/>()
    /// </summary>
public interface IHasDoubleClick
    {
        GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e);
    }
}

// USE THE BELOW on your component!
//
//
//public override void CreateAttributes()
//{
//    m_attributes = new GH_DoubleClickAttributes(this);

//}
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Components.Templates
{
    public class GH_DoubleClickAttributes : GH_ComponentAttributes
    {
        readonly IHasDoubleClick component;

        public GH_DoubleClickAttributes(IGH_Component component)
          : base(component)
        {
            this.component = component as IHasDoubleClick;
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                component.OnDoubleClick(sender, e);
            }

            base.RespondToMouseDoubleClick(sender, e);
            //return base.RespondToMouseDoubleClick(sender, e);
            return GH_ObjectResponse.Handled;


        }
    }
}

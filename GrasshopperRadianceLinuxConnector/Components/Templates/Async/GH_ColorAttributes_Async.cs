using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrasshopperRadianceLinuxConnector
{
    /// <summary>
    /// This class sets colors for the grasshopper components
    /// https://discourse.mcneel.com/t/change-the-color-of-the-custom-component/56435/2
    /// </summary>
    public class GH_ColorAttributes_Async : GH_ComponentAttributes
    {
        GH_TemplateAsync component;

        public GH_ColorAttributes_Async(IGH_Component component)
          : base(component)
        {
            this.component = component as GH_TemplateAsync;

            palette_normal_standard = GH_Skin.palette_normal_standard;
            palette_normal_selected = GH_Skin.palette_normal_selected;
            palette_hidden_standard = GH_Skin.palette_hidden_standard;
            palette_hidden_selected = GH_Skin.palette_hidden_selected;
        }

        public GH_PaletteStyle palette_normal_standard;
        public GH_PaletteStyle palette_normal_selected;
        public GH_PaletteStyle palette_hidden_standard;
        public GH_PaletteStyle palette_hidden_selected;
        public GH_PaletteStyle ColorUnselected { get; set; }
        public GH_PaletteStyle ColorSelected { get; set; }


        /// <summary>
        /// Renders the running components in another color
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="channel"></param>
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Objects && component.PhaseForColors != GH_TemplateAsync.AestheticPhase.NotRunning)
            {

                // Swap out palette for normal, unselected components.
                GH_Skin.palette_normal_standard = ColorUnselected;
                GH_Skin.palette_hidden_standard = ColorUnselected;
                GH_Skin.palette_normal_selected = ColorSelected;
                GH_Skin.palette_hidden_selected = ColorSelected;

                base.Render(canvas, graphics, channel);

                // Put the original style back.
                GH_Skin.palette_normal_standard = palette_normal_standard;
                GH_Skin.palette_normal_selected = palette_normal_selected;
                GH_Skin.palette_hidden_standard = palette_hidden_standard;
                GH_Skin.palette_hidden_selected = palette_hidden_selected;
            }
            else
                base.Render(canvas, graphics, channel);
        }
    }
}

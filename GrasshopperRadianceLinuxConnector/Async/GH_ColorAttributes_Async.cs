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
        }



        /// <summary>
        /// Renders the running components in another color
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="channel"></param>
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Objects && component.PhaseForColors == GH_TemplateAsync.AestheticPhase.Running)
            {
                // Cache the existing style.
                GH_PaletteStyle style = GH_Skin.palette_normal_standard;
                GH_PaletteStyle selectedStyle = GH_Skin.palette_normal_selected;
                GH_PaletteStyle styleHidden = GH_Skin.palette_hidden_standard;
                GH_PaletteStyle styleHiddenSelected = GH_Skin.palette_hidden_selected;

                // Swap out palette for normal, unselected components.
                GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.Purple);
                GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.Purple);
                
                GH_Skin.palette_normal_selected = new GH_PaletteStyle(Color.MediumVioletRed);
                GH_Skin.palette_hidden_selected = new GH_PaletteStyle(Color.MediumVioletRed);

                base.Render(canvas, graphics, channel);

                // Put the original style back.
                GH_Skin.palette_normal_standard = style;
                GH_Skin.palette_normal_selected = selectedStyle;
                GH_Skin.palette_hidden_standard = styleHidden;
                GH_Skin.palette_hidden_selected = styleHiddenSelected;
            }
            else
                base.Render(canvas, graphics, channel);
        }
    }
}

using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    /// <summary>
    /// This class sets colors for the grasshopper components
    /// https://discourse.mcneel.com/t/change-the-color-of-the-custom-component/56435/2
    /// </summary>
    public class GH_TestComponentColor_Attr : GH_ComponentAttributes
    {

        public GH_TestComponentColor_Attr(IGH_Component component)
          : base(component)
        {

        }

        public System.Drawing.Color ColorSelected { get; set; } = System.Drawing.Color.White;
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Black;



        /// <summary>
        /// Renders the running components in another color
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="channel"></param>
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {

            // Cache the existing style.
            GH_PaletteStyle style = GH_Skin.palette_normal_standard;
            GH_PaletteStyle selectedStyle = GH_Skin.palette_normal_selected;
            GH_PaletteStyle styleHidden = GH_Skin.palette_hidden_standard;
            GH_PaletteStyle styleHiddenSelected = GH_Skin.palette_hidden_selected;

            // Swap out palette for normal, unselected components.
            GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color);
            GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color);

            GH_Skin.palette_normal_selected = new GH_PaletteStyle(ColorSelected);
            GH_Skin.palette_hidden_selected = new GH_PaletteStyle(ColorSelected);

            base.Render(canvas, graphics, channel);

            // Put the original style back.
            GH_Skin.palette_normal_standard = style;
            GH_Skin.palette_normal_selected = selectedStyle;
            GH_Skin.palette_hidden_standard = styleHidden;
            GH_Skin.palette_hidden_selected = styleHiddenSelected;

        }
    }
}

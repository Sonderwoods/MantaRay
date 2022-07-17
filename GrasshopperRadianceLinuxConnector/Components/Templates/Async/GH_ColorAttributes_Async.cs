using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
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

        Pen penTrueSelected = new Pen(Color.FromArgb(255, Color.DarkGreen), 4f);
        Pen penTrueSelectedTree = new Pen(Color.FromArgb(255, Color.DarkGreen), 4f) { DashStyle = DashStyle.Dash };
        Pen penTrueUnselected = new Pen(Color.FromArgb(80, Color.DarkGreen), 4f);
        Pen penTrueUnselectedTree = new Pen(Color.FromArgb(80, Color.DarkGreen), 4f) {  DashStyle = DashStyle.Dash};
        Pen penFalseSelected = new Pen(Color.FromArgb(255, Color.DarkRed), 4f);
        Pen penFalseSelectedTree = new Pen(Color.FromArgb(255, Color.DarkRed), 4f) { DashStyle = DashStyle.Dash };
        Pen penFalseUnselected = new Pen(Color.FromArgb(80, Color.DarkRed), 4f);
        Pen penFalseUnselectedTree = new Pen(Color.FromArgb(80, Color.DarkRed), 4f) { DashStyle = DashStyle.Dash };


        /// <summary>
        /// Renders the running components in another color
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="channel"></param>
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            switch (channel)
            {
                case GH_CanvasChannel.First:
                    if (Selected)
                    {
                        RenderInputComponentBoxes(graphics);
                    }
                    break;
                case GH_CanvasChannel.Objects:
                    DrawObjects(canvas, graphics, channel);
                    break;
                case GH_CanvasChannel.Wires:
                    DrawWires(canvas, graphics);
                    break;
                default:
                    base.Render(canvas, graphics, channel);
                    break;
            }

        }

        private void DrawObjects(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            switch (component.PhaseForColors)
            {
                case GH_TemplateAsync.AestheticPhase.Running:
                case GH_TemplateAsync.AestheticPhase.Reusing:
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

                    break;

                default:
                    base.Render(canvas, graphics, channel);
                    break;

            }

        }

        /// <summary>
        /// From TUNNY plugin
        /// </summary>
        /// <param name="graphics"></param>
        private void RenderInputComponentBoxes(Graphics graphics)
        {
            Brush[] fill = new[]
            {
                    new SolidBrush(Color.FromArgb(170, Color.DarkBlue)),
                    new SolidBrush(Color.FromArgb(170, Color.Green)),
            };

            Pen[] edge = new[] { Pens.DarkBlue, Pens.Green };


            Brush[] fillBool = new[]
            {
                    new SolidBrush(Color.FromArgb(170, Color.DarkGreen)),
                    new SolidBrush(Color.FromArgb(170, Color.DarkRed)),
            };

            Pen[] edgeBool = new[] { Pens.DarkGreen, Pens.DarkRed };

            for (int i = 1; i < 2; i++)
            {
                foreach (IGH_Param source in Owner.Params.Input[i].Sources)
                {
                    Guid guid = source.InstanceGuid;

                    bool? isPos = IsPositiveParam(Owner.Params.Input[i]);

                    if (isPos != null)
                    {
                        RenderBox(graphics, fillBool[isPos.Value ? 0 : 1], edgeBool[isPos.Value ? 0 : 1], guid);
                    }
                    else
                    {
                        RenderBox(graphics, fill[i], edge[i], guid);
                    }


                }
            }
        }

        private bool? IsPositiveParam(IGH_Param param)
        {
            switch (param)
            {
                case Param_Boolean p:
                    return p.VolatileData.AllData(false).All(b => b is GH_Boolean v && v.IsValid && v.Value == true);
                    
 
                case Param_String p:
                    return p.VolatileData.AllData(false)
                        .All(b => b is GH_String v && v.IsValid 
                        && (string.Equals(v.Value, "true", StringComparison.InvariantCultureIgnoreCase)
                        || (double.TryParse(v.Value, out double r) && r > 1.0)));

                case Param_Number p:
                    return p.VolatileData.AllData(false)
                        .All(b => b is GH_Number v && v.IsValid && v.Value >= 1.0);

                case Param_Integer p:
                    return p.VolatileData.AllData(false)
                        .All(b => b is GH_Integer v && v.IsValid && v.Value >= 1);

                default:
                    return null;
            }
        }

        /// <summary>
        /// From TUNNY plugin
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="fill"></param>
        /// <param name="edge"></param>
        /// <param name="guid"></param>
        private void RenderBox(Graphics graphics, Brush fill, Pen edge, Guid guid)
        {
            GH_Document doc = Owner.OnPingDocument();

            if (doc == null) return;

            IGH_DocumentObject obj = doc.FindObject(guid, false);
            if (obj == null) return;

            if (!obj.Attributes.IsTopLevel)
            {
                Guid topLevelGuid = obj.Attributes.GetTopLevel.InstanceGuid;
                obj = doc.FindObject(topLevelGuid, true);
            }
            var rectangle = GH_Convert.ToRectangle(obj.Attributes.Bounds);
            rectangle.Inflate(6, 6);
            graphics.FillRectangle(fill, rectangle);
            graphics.DrawRectangle(edge, rectangle);
        }

        /// <summary>
        /// From TUNNY plugin
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        private void DrawWires(GH_Canvas canvas, Graphics graphics)
        {
            Pen[] pensSelected = new[]
                {
                    new Pen(Color.DarkBlue, 5f),
                    new Pen(Color.Green, 3f)
                };
            Pen[] pensUnselected = new[]

                {
                    new Pen(Color.FromArgb(120, Color.DarkBlue), 4f),
                    new Pen(Color.FromArgb(120, Color.Green), 2f)
                };

            for (int i = 0; i < 2; i++)
            {
                DrawPath(canvas, graphics, Owner.Params.Input[i], pensSelected[i], pensUnselected[i]);
            }

            pensSelected[0].Dispose();
            pensSelected[1].Dispose();
            pensUnselected[0].Dispose();
            pensUnselected[1].Dispose();

        }

        /// <summary>
        /// From TUNNY plugin
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="param"></param>
        private void DrawPath(GH_Canvas canvas, Graphics graphics, IGH_Param param, Pen wirePenSelected, Pen wirePenUnselected)
        {
            PointF p1 = param.Attributes.InputGrip;

            foreach (IGH_Param source in param.Sources)
            {
                PointF p0 = source.Attributes.OutputGrip;

                if (!canvas.Painter.ConnectionVisible(p0, p1)) continue;

                using (GraphicsPath wirePath = GH_Painter.ConnectionPath(p0, p1, GH_WireDirection.right, GH_WireDirection.left))
                {
                    if (wirePath == null) continue;

                    bool? isPos = IsPositiveParam(param);

                    if(isPos.HasValue)
                    {
                        if(isPos.Value)
                        {
                            if(param.Access == GH_ParamAccess.tree && param.VolatileData.PathCount > 1)
                            graphics.DrawPath(source.Attributes.Selected || Owner.Attributes.Selected ? penTrueSelectedTree : penTrueUnselectedTree, wirePath);
                            else

                            graphics.DrawPath(source.Attributes.Selected || Owner.Attributes.Selected ? penTrueSelected : penTrueUnselected, wirePath);
                        }
                        else
                        {
                            if (param.Access == GH_ParamAccess.tree && param.VolatileData.PathCount > 1)
                                graphics.DrawPath(source.Attributes.Selected || Owner.Attributes.Selected ? penFalseSelectedTree : penFalseUnselectedTree, wirePath);
                            else
                                graphics.DrawPath(source.Attributes.Selected || Owner.Attributes.Selected ? penFalseSelected : penFalseUnselected, wirePath);
                        }
                    }
                    else
                    {
                        graphics.DrawPath(source.Attributes.Selected || Owner.Attributes.Selected ? wirePenSelected : wirePenUnselected, wirePath);
                    }

                    //wirePen.Dispose();


                }

            }

        }

    }

}

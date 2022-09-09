using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using MantaRay.Components.Templates;
using MantaRay.OldComponents;
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
    public class GH_ColorAttributes_Async : GH_DoubleClickAttributes
    {
        readonly IGH_Component component;
        //readonly GH_Template_Async component;
        readonly IHasDoubleClick doubleClickComponent;

        public GH_ColorAttributes_Async(IGH_Component component)
          : base(component)
        {
            this.component = component;
            this.doubleClickComponent = component as IHasDoubleClick;

            palette_normal_standard = GH_Skin.palette_normal_standard;
            palette_normal_selected = GH_Skin.palette_normal_selected;
            palette_hidden_standard = GH_Skin.palette_hidden_standard;
            palette_hidden_selected = GH_Skin.palette_hidden_selected;


            FontFamily fontFamily = new FontFamily("Arial");
            font = new Font(
               fontFamily,
               14,
               FontStyle.Bold,
               GraphicsUnit.Pixel);
        }

        public Font font;

        public GH_PaletteStyle palette_normal_standard;
        public GH_PaletteStyle palette_normal_selected;
        public GH_PaletteStyle palette_hidden_standard;
        public GH_PaletteStyle palette_hidden_selected;
        public GH_PaletteStyle ColorUnselected { get; set; }
        public GH_PaletteStyle ColorSelected { get; set; }



        public enum PenWireTypes
        {
            None = 0,
            Tree = 1,
            Selected = 2,
            True = 4,
            Green = 8
        }

        public Pen GetPen(PenWireTypes type) => pens[(int)type];

        private readonly Pen[] pens = new Pen[]
        {
            new Pen(Color.FromArgb(80, Color.DarkBlue), 4f), //BluePenFalseUnselected
            new Pen(Color.FromArgb(80, Color.DarkBlue), 4f) { DashStyle = DashStyle.Dash }, //BluePenFalseUnselectedTree
            new Pen(Color.FromArgb(255, Color.DarkBlue), 4f), //BluePenFalseSelected
            new Pen(Color.FromArgb(255, Color.DarkBlue), 4f) { DashStyle = DashStyle.Dash }, //BluePenFalseSelectedTree

            new Pen(Color.FromArgb(80, Color.DarkBlue), 4f), //BluePenTrueUnselected
            new Pen(Color.FromArgb(80, Color.DarkBlue), 4f) { DashStyle = DashStyle.Dash }, //BluePenTrueUnselectedTree
            new Pen(Color.FromArgb(255, Color.DarkBlue), 4f), //BluePenTrueSelected
            new Pen(Color.FromArgb(255, Color.DarkBlue), 4f) { DashStyle = DashStyle.Dash }, //BluePenTrueSelectedTree


            new Pen(Color.FromArgb(80, Color.DarkRed), 4f), //penFalseUnselected
            new Pen(Color.FromArgb(80, Color.DarkRed), 4f) { DashStyle = DashStyle.Dash }, //penFalseUnselectedTree
            new Pen(Color.FromArgb(255, Color.DarkRed), 4f), //penFalseSelected
            new Pen(Color.FromArgb(255, Color.DarkRed), 4f) { DashStyle = DashStyle.Dash }, //penFalseSelectedTree

            new Pen(Color.FromArgb(80, Color.DarkGreen), 4f), //penTrueUnselected
            new Pen(Color.FromArgb(80, Color.DarkGreen), 4f) { DashStyle = DashStyle.Dash }, //penTrueUnselectedTree
            new Pen(Color.FromArgb(255, Color.DarkGreen), 4f), // penTrueSelected
            new Pen(Color.FromArgb(255, Color.DarkGreen), 4f) { DashStyle = DashStyle.Dash }, //penTrueSelectedTree
            
        };


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
                    if (component != null && component is GH_Template_Async_Extended c && !string.IsNullOrEmpty(c.LogName))
                    {
                        RenderText(c.LogName, graphics);
                    }

                    break;
                default:
                    base.Render(canvas, graphics, channel);
                    break;
            }

        }

        private void DrawObjects(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (component != null && component is GH_Template_Async_Extended c)
            {
                switch (c.PhaseForColors)
                {
                    case GH_Template_Async_Extended.AestheticPhase.Running:
                    case GH_Template_Async_Extended.AestheticPhase.Reusing:
                    case GH_Template_Async_Extended.AestheticPhase.Cancelled:
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
            else if (component != null && component is GH_Template_Async_OBSOLETE o)
            {
                switch (o.PhaseForColors)
                {
                    case GH_Template_Async_OBSOLETE.AestheticPhase.Running:
                    case GH_Template_Async_OBSOLETE.AestheticPhase.Reusing:
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
            else
            {
                base.Render(canvas, graphics, channel);
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
        /// <param name="graphics"></param>
        /// <param name="fill"></param>
        /// <param name="edge"></param>
        /// <param name="guid"></param>
        private void RenderText(string s, Graphics graphics)
        {
            if (string.IsNullOrEmpty(s))
                return;

            const int MAXLEN = 25;
            GH_Document doc = Owner.OnPingDocument();

            if (doc == null) return;

            RectangleF rectangle = Bounds;
            rectangle.Y -= 33;
            rectangle.Width += 40;
            if (s.Length > MAXLEN)
            {
                s = s.Substring(0, MAXLEN - 1) + "...";
            }
            //rectangle.Inflate(6, 6);
            graphics.DrawString(s, font, new SolidBrush(Selected ? (ColorSelected?.Fill ?? GH_Skin.palette_normal_standard.Fill) : (ColorUnselected?.Fill ?? GH_Skin.palette_normal_standard.Fill)), rectangle);
            //graphics.FillRectangle(fill, rectangle);
            //graphics.DrawRectangle(edge, rectangle);
        }

        /// <summary>
        /// From TUNNY plugin
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        private void DrawWires(GH_Canvas canvas, Graphics graphics)
        {
            //Pen penSelected = new Pen(Color.DarkBlue, 5f);
            //Pen penUnselected = new Pen(Color.FromArgb(120, Color.DarkBlue), 4f);

            if (Owner.Params.Input[0].WireDisplay == GH_ParamWireDisplay.@default)
            {
                DrawPath(canvas, graphics, Owner.Params.Input[0], PenWireTypes.None); // First input wire BLUE
            }
            else
            {
                Owner.Params.Input[0].Attributes.RenderToCanvas(canvas, GH_CanvasChannel.Wires);
            }

            if (Owner.Params.Input[1].WireDisplay == GH_ParamWireDisplay.@default)
            {
                DrawPath(canvas, graphics, Owner.Params.Input[1], PenWireTypes.Green); // Second input wire
            }
            else
            {
                Owner.Params.Input[1].Attributes.RenderToCanvas(canvas, GH_CanvasChannel.Wires);
            }

            //penSelected.Dispose();
            //penUnselected.Dispose();

        }

        /// <summary>
        /// From TUNNY plugin
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="param"></param>
        private void DrawPath(GH_Canvas canvas, Graphics graphics, IGH_Param param, PenWireTypes penTypes)
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


                    if (isPos.HasValue && isPos.Value)
                    {
                        penTypes |= PenWireTypes.True;
                    }

                    if (source.Attributes.Selected || Owner.Attributes.Selected)
                    {
                        penTypes |= PenWireTypes.Selected;
                    }

                    if ((/*param.Access ==  GH_ParamAccess.tree && */ param.VolatileData.PathCount > 1) || (param.DataMapping == GH_DataMapping.Graft))
                    {
                        penTypes |= PenWireTypes.Tree;
                    }

                    graphics.DrawPath(GetPen(penTypes), wirePath);



                    //wirePen.Dispose();


                }

            }

        }


    }

}

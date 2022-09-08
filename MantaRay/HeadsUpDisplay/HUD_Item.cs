using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer.HeadsUpDisplay
{

    public class HUD_Item
    {
        /// <summary>
        /// In our case this could be the radiance object. Hint Hint. But you can make your own objects too!
        /// </summary>
        public IHasPreview Value { get; set; }
        public HUD HUD { get; set; }
        public string Name => Value.GetName();
        public string Description => Value.GetDescription();
        public Rectangle Rectangle { get; set; }
        public BoundingBox? Box => Value.GetBoundingBox();

        public Color Color { get; set; } = Color.White;
        public Dictionary<string, HUD_ContextMenuEventHandler> ContextMenuItems { get; set; } = new Dictionary<string, HUD_ContextMenuEventHandler>();
        public HUD_ContextMenuEventHandler OnLeftClick { get; set; }


        public HUD_Item(IHasPreview value)
        {
            Value = value;
        }

        /// <summary>
        /// Only use this instanciator if you can set the value manually!
        /// </summary>
        public HUD_Item()
        {

        }


        public void ZoomToBox()
        {
            if (Box != null && Box.Value.IsValid)
            {
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(Box.Value);
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
            }
        }

        public override string ToString() => Name;

        virtual public void DrawEdges(IGH_PreviewArgs args) => Value.DrawWires(args, 1);
   

        virtual public void DrawMesh(IGH_PreviewArgs args, DisplayMaterial material = null, Color? colorOverride = null)
        {

            material = material ?? new DisplayMaterial(colorOverride ?? Color);
            material.Diffuse = colorOverride ?? Color;
            material.Emission = material.Diffuse;

            Value.DrawPreview(args, material);

        }



        /// <summary>
        /// This shows the box with name on... 
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="HUD"></param>
        /// <param name="args"></param>
        public virtual void Draw2D(ref System.Drawing.Point anchor, HUD HUD, DrawEventArgs args)
        {
            HUD = HUD ?? this.HUD;
            if (HUD == null)
                return;

            bool isItemHighlighted = ReferenceEquals(HUD.HighlightedItem, this);

            Color color = isItemHighlighted ? Color.FromArgb(190, Color) : Color.FromArgb(220, Color);

            Rectangle = new Rectangle(
            x: (int)(anchor.X * HUD.Scale),
            y: (int)(anchor.Y * HUD.Scale),
            width: (int)(HUD.Width * HUD.Scale),
            height: (int)(HUD.Height * HUD.Scale));

            args.Display.Draw2dRectangle(Rectangle, Color.White, isItemHighlighted ? 2 : 0, color);

            args.Display.Draw2dText(
                text: Name,
                color: Color.Black,
                screenCoordinate: new Point2d(
                    x: anchor.X + 4 * HUD.Scale,
                    y: anchor.Y + (HUD.Height / 2.0 - HUD.TextSize / 2.0 + 1.0) * HUD.Scale),
                middleJustified: false,
                height: HUD.TextSize,
                fontface: HUD.FontName
            );


            anchor.Y += (int)((HUD.Height + 2) * HUD.Scale);
        }


        /// <summary>
        /// This draws the box showing the description
        /// </summary>
        /// <param name="HUD"></param>
        /// <param name="args"></param>
        internal virtual void DrawDescription(HUD HUD, DrawEventArgs args)
        {

            HUD = HUD ?? this.HUD;
            if (HUD == null)
                return;


            Point2d anchor = new Point2d(
                (int)(HUD.Anchor.X + HUD.Scale * ((HUD.HUDs.Any(h => h.Value.Enabled && !h.Value.Collapsed) ? HUD.Width : 0) + 10)),
                HUD.Anchor.Y - 10
                );
            int fontSize = (int)(HUD.TextSize * HUD.Scale);

            Rectangle textSize = args.Display.Measure2dText(Description + "\n\n.", anchor, false, 0, fontSize, HUD.FontDescription);
            textSize.Width += 25;
            textSize.Height = Math.Abs(textSize.Height + 20);


            args.Display.Draw2dRectangle(textSize, Color.Black, 0, Color.FromArgb(200, 255, 255, 255));

            //args.Display.Draw2dRectangle(new PointF(40f,40f), width, height, 5, Color.Black, 0, Color.FromArgb(220, Color.White));

            args.Display.Draw2dText(Description, Color.Black, anchor + new Point2d(10, 20),
                false, fontSize, HUD.FontDescription);




        }

        /// <summary>
        /// Used to pass right click menu on the item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void HUD_ContextMenuEventHandler(object sender, HUD_ItemEventArgs e);


        public class HUD_ItemEventArgs : EventArgs
        {
            public HUD_Item Item { get; set; }

            public HUD_ItemEventArgs(HUD_Item item)
            {
                Item = item;
            }
        }
    }
}

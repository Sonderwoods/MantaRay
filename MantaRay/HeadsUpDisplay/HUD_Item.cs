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
        public object Sender { get; set; }
        public HUD HUD { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Rectangle Rectangle { get; set; }
        public BoundingBox? Box { get; set; }
        public Mesh Mesh
        {
            get => _mesh;
            set
            {
                _mesh = value;
                Box = _mesh.GetBoundingBox(false);
            }
        }
        private Mesh _mesh;
        public Color Color { get; set; } = Color.White;
        public Dictionary<string, HUD_ContextMenuEventHandler> ContextMenuItems { get; set; } = new Dictionary<string, HUD_ContextMenuEventHandler>();
        public HUD_ContextMenuEventHandler OnLeftClick { get; set; }
        public void ZoomToBox()
        {
            if (Box != null && Box.Value.IsValid)
            {
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(Box.Value);
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

            }
        }

        public override string ToString()
        {
            return Name;
        }

        virtual public void DrawEdges(IGH_PreviewArgs args)
        {
            if (Mesh != null && Mesh.IsValid)
                args.Display.DrawMeshWires(Mesh, Color.Black);
        }

        virtual public void DrawMesh(IGH_PreviewArgs args, double alpha = 0.3, bool twoSided = false, bool grey = false)
        {




            if (Mesh?.IsValid == true)
            {
                if (Mesh.VertexColors.Count == 0)
                {

                    args.Display.DrawMeshShaded(Mesh,
                        new DisplayMaterial(grey ? Color.Gray : Color)
                        {
                            Transparency = twoSided ? 0.0 : 1 - alpha,
                            Emission = grey ? Color.Gray : Color,
                            IsTwoSided = twoSided,
                                //IsTwoSided = true,
                            BackDiffuse = Color.Black,
                            BackEmission = Color.Black,
                            BackTransparency = twoSided ? 0.3 : 1 - alpha,
                        });


                }
                else
                {
                    args.Display.DrawMeshFalseColors(Mesh);
                }
            }
        }



        public virtual void Draw(ref System.Drawing.Point anchor, HUD HUD, DrawEventArgs args)
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

        internal virtual void DrawDescription(HUD HUD, DrawEventArgs args)
        {

            HUD = HUD ?? this.HUD;
            if (HUD == null)
                return;


            Point2d anchor = new Point2d(
                (int)(HUD.Anchor.X + HUD.Scale * ((HUD.Collapsed ? 0 : HUD.Width) + 10)),
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

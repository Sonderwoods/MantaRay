using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrasshopperRadianceLinuxConnector
{
    public class HUD
    {
        private bool enabled = true;

        public string FontName { get; set; } = "Arial Rounded MT Bold";
        public string FontDescription { get; set; } = "Arial Unicode MS";

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
        System.Drawing.Point Anchor { get; set; } = new System.Drawing.Point(40, 40);

        public int Width { get; set; } = 200;
        public int Height { get; set; } = 25;
        public int TextSize { get; set; } = 15;

        public HUD_MouseCallback Callback { get; set; }
        public HUD_Item HighlightedItem { get; set; }
        public List<HUD_Item> Items { get; set; } = new List<HUD_Item>();
        public double Scale { get; set; } = 1.0;

        public void Draw(DrawEventArgs args)
        {
            if (!Enabled || Items?.Count == 0) return;
            if (args.Viewport.Name != Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name) return;
            Callback.Enabled = true;

            System.Drawing.Point CurrentAnchor = Anchor;


            foreach (var item in Items)
            {
                item.Draw(ref CurrentAnchor, this, args);
            }

            HighlightedItem.DrawDescription(ref CurrentAnchor, this, args);

        }

        public HUD()
        {
          Callback  = new HUD_MouseCallback(this);
        }

        public HUD(IGH_Component component)
        {
            Callback = new HUD_MouseCallback(this, component);
        }

        private void SetHighlightedItem(int x, int y)
        {
            HighlightedItem = Items.FirstOrDefault(item => item.Rectangle.Contains(x, y));
        }







        public class HUD_Item
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Rectangle Rectangle { get; set; }
            public BoundingBox? Box { get; set; }
            public Mesh Mesh { get; set; }
            public Color Color { get; set; } = Color.White;
            public Dictionary<string, HUD_ContextMenuEventHandler> ContentMenuItems { get; set; } = new Dictionary<string, HUD_ContextMenuEventHandler>();
            public void ZoomToBox()
            {
                if (Box != null && Box.Value.IsValid)
                {
                    Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(Box.Value);
                    Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

                }
            }

            virtual public void ShowWireframe(DrawEventArgs args)
            {
                if (Mesh?.IsValid == true)
                {
                    if (Mesh.VertexColors.Count == 0)
                    {
                        args.Display.DrawMeshShaded(Mesh, new DisplayMaterial(Color));

                    }
                    else
                    {
                        args.Display.DrawMeshFalseColors(Mesh);
                    }
                }
            }

            internal void Draw(ref System.Drawing.Point anchor, HUD HUD, DrawEventArgs args)
            {
                bool isItemHighlighted = Object.ReferenceEquals(HUD.HighlightedItem, this);

                Color color = isItemHighlighted ? Color.FromArgb(235, 235, 235, 235) : Color;

                Rectangle = new Rectangle(
                x: (int)(anchor.X * HUD.Scale),
                y: (int)(anchor.Y * HUD.Scale),
                width: (int)(HUD.Width * HUD.Scale),
                height: (int)(HUD.Height * HUD.Scale));

                args.Display.Draw2dRectangle(Rectangle, Color.Black, 0, isItemHighlighted ? Color.FromArgb(235, 235, 235, 235) : Color);

                args.Display.Draw2dText(
                    text: Name,
                    color: Color.Black,
                    screenCoordinate: new Point2d(anchor.X + (3 * HUD.Scale), anchor.Y + ((HUD.Height / 2.0 - HUD.TextSize / 2.0) * HUD.Scale)),
                    middleJustified: false,
                    height: HUD.TextSize,
                    fontface: HUD.FontName
                );


                anchor.Y += (int)((HUD.Height + 2) * HUD.Scale);
            }

            internal void DrawDescription(ref System.Drawing.Point anchor, HUD HUD, DrawEventArgs args)
            {
               

                if (object.ReferenceEquals(args.Viewport, HUD.Callback.ActiveViewport))
                {

                    Point2d topLeftCorner = new Point2d(HUD.Callback.LastMousePoint.X + 15, HUD.Callback.LastMousePoint.Y);

                    Rectangle rectDesc = args.Display.Measure2dText(Description, topLeftCorner, false, 0, (int)(HUD.TextSize * HUD.Scale), HUD.FontDescription);


                    int width = rectDesc.Width + 15;
                    int height = rectDesc.Height + 25;

                    args.Display.DrawRoundedRectangle(new PointF((float)topLeftCorner.X + width / 2,
                        (float)topLeftCorner.Y + height / 2), width, height, 5, Color.Black, 0, Color.FromArgb(220, Color.White));


                    args.Display.Draw2dText(Description, Color.Black, topLeftCorner + new Rhino.Geometry.Point2d(0, 10),
                        false, (int)(HUD.TextSize * HUD.Scale), HUD.FontDescription);

                }

             
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


        public class HUD_MouseCallback : Rhino.UI.MouseCallback
        {
            public HUD HUD { get; set; }
            public string ActiveViewportName { get; set; }

            public RhinoViewport ActiveViewport { get; set; }
            public IGH_Component Component { get; set; }
            public System.Drawing.Point LastMousePoint { get; set; }

            public HUD_MouseCallback() { }
            public HUD_MouseCallback(HUD HUD) { this.HUD = HUD; }
            public HUD_MouseCallback(HUD HUD, IGH_Component component) { this.HUD = HUD; Component = component; }

            protected override void OnMouseEnter(MouseCallbackEventArgs e) => ActiveViewportName = e.View?.ActiveViewport?.Name;
            protected override void OnMouseMove(MouseCallbackEventArgs e)
            {
                if (HUD == null)
                    Enabled = false;

                if (Component != null)
                {
                    Enabled = !(Component.Locked || Component.Hidden);
                    
                }

                HUD.SetHighlightedItem(e.ViewportPoint.X, e.ViewportPoint.Y);
                LastMousePoint = e.ViewportPoint;
                Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                base.OnMouseMove(e);
            }
            protected override void OnMouseDown(MouseCallbackEventArgs e)
            {

                if (!(e.MouseButton == MouseButton.Right && HUD.HighlightedItem != null))
                {
                    e.Cancel = false;
                    return;
                }
                else
                {
                    e.Cancel = true;
                    ContextMenuStrip menu = new ContextMenuStrip();
                    if (HUD.HighlightedItem?.Box != null)
                    {
                        ToolStripMenuItem menuZoom = new ToolStripMenuItem("Zoom");
                        menuZoom.Click += (s, ee) => HUD.HighlightedItem.ZoomToBox();
                        menu.Items.Add(menuZoom);
                    }


                    foreach (var item in HUD.HighlightedItem.ContentMenuItems)
                    {
                        ToolStripMenuItem menuSelectGH = new ToolStripMenuItem(item.Key);
                        menuSelectGH.Click += (s, ee) => { item.Value(item.Value, new HUD_Item.HUD_ItemEventArgs(HUD.HighlightedItem)); };
                        menu.Items.Add(menuSelectGH);
                    }

                    if (menu.Items.Count > 0)
                    {
                        menu.Show(Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ClientToScreen(e.ViewportPoint));
                    }


                }

                //Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                base.OnMouseDown(e);
            }
        }





    }
}

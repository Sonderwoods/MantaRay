using Grasshopper.Kernel;
using MantaRay.Components;
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

namespace MantaRay
{
    public class HUD
    {
        public bool Collapsed = false;
        private bool enabled = true;

        public string FontName { get; set; } = "Arial Rounded MT Bold";
        public string FontDescription { get; set; } = "Arial Unicode MS";

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
        System.Drawing.Point Anchor { get; set; } = new System.Drawing.Point(50, 40);

        public int Width { get; set; } = 200;
        public int Height { get; set; } = 25;
        public int TextSize { get; set; } = 15;

        public IGH_Component Component { get; set; }

        public HUD_MouseCallback Callback { get; set; }
        public HUD_Item HighlightedItem
        {
            get => _highlightedItem;
            set
            {
                if (!Object.ReferenceEquals(_highlightedItem, value))
                {
                    HighlightedItemChanged?.Invoke(this, new EventArgs());
                    _highlightedItem = value;
                }
            }
        }

        public event EventHandler HighlightedItemChanged;
        public HUD_Item _highlightedItem;
        public List<HUD_Item> Items { get; set; } = new List<HUD_Item>() { };
        public HUD_Item CloseBtn;

        public double Scale { get; set; } = 1.0;

        public virtual void Draw(DrawEventArgs args)
        {
            if (!Enabled || Items?.Count == 0) return;
            if (args.Viewport.Name != Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name) return;
            Callback.Enabled = true;

            System.Drawing.Point CurrentAnchor = Anchor;

            if (!Collapsed)
            {
                foreach (var item in Items)
                {
                    item?.Draw(ref CurrentAnchor, this, args);
                }

            }
            else
            {
                Items.ForEach(item => item.Rectangle = default);
            }

            HighlightedItem?.DrawDescription(this, args);

            CloseBtn?.Draw(ref CurrentAnchor, this, args);


        }

        public HUD()
        {
            Callback = new HUD_MouseCallback(this);
            CloseBtn = new HUD_CloseButton(this);


        }

        public HUD(IGH_Component component) : base()
        {
            Component = component;

        }

        private void SetHighlightedItem(int x, int y, MouseCallbackEventArgs e = null)
        {
            if (e != null && e.View.ActiveViewport.Name != Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name)
            {
                HighlightedItem = null;
            }
            else
            {
                HighlightedItem = Items.FirstOrDefault(item => item.Rectangle.Contains(x, y));
                if (CloseBtn?.Rectangle.Contains(x, y) ?? false)
                    HighlightedItem = CloseBtn;
            }
        }


        public class HUD_CloseButton : HUD_Item
        {

            public HUD_CloseButton(HUD hud)
            {
                this.HUD = hud;
                Description = "Click to hide\nRight click to remove";
                Color = Color.FromArgb(200, 255, 255, 255);
                Name = "X";
                OnLeftClick = new HUD_ContextMenuEventHandler((e, args) =>
                 {
                     HideCollapse(hud);

                 });

                ContextMenuItems.Add("Remove", new HUD_ContextMenuEventHandler((e, args) =>
                {
                    if (this.HUD != null && this.HUD.Component != null)
                        this.HUD.Component.Hidden = true;
                    Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                    Grasshopper.Instances.RedrawCanvas();

                }));



            }



            public void HideCollapse(HUD hud = null)
            {
                HUD _hud = hud ?? this.HUD ?? null;
                if (_hud != null)
                {

                    _hud.CloseBtn.Name = _hud.Collapsed ? "X" : "+";
                    _hud.Collapsed = !_hud.Collapsed;
                    _hud.CloseBtn.Description = _hud.Collapsed ? "Click to Expand\nRight click to remove" : "Click to hide\nRight click to remove";
                    Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                }
            }
            public int Size { get; set; } = 20;


            public new Mesh Mesh { get => null; }


            public override void Draw(ref System.Drawing.Point anchor, HUD HUD, DrawEventArgs args)
            {
                int fontSize = 10;
                bool isItemHighlighted = Object.ReferenceEquals(HUD.HighlightedItem, this);

                Color color = isItemHighlighted ? Color.FromArgb(190, Color) : Color.FromArgb(220, Color);

                Rectangle = new Rectangle(
                x: (int)(HUD.Anchor.X - (Size + 5) * HUD.Scale),
                y: (int)(HUD.Anchor.Y),
                width: (int)(Size * HUD.Scale),
                height: (int)(Size * HUD.Scale));

                args.Display.Draw2dRectangle(Rectangle, Color.White, isItemHighlighted ? 2 : 0, color);

                args.Display.Draw2dText(
                    text: Name,
                    color: Color.Black,
                    screenCoordinate: new Point2d(HUD.Anchor.X - ((5 + Size / 2.0) * HUD.Scale), HUD.Anchor.Y + ((fontSize) * HUD.Scale)),
                    middleJustified: true,
                    height: fontSize,
                    fontface: HUD.FontName
                );


                anchor.Y += (int)((HUD.Height + 2) * HUD.Scale);

                //base.Draw(ref anchor, HUD, args);
            }




        }




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

                bool isItemHighlighted = Object.ReferenceEquals(HUD.HighlightedItem, this);

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
                        x: anchor.X + (4 * HUD.Scale),
                        y: anchor.Y + ((HUD.Height / 2.0 - HUD.TextSize / 2.0 + 1.0) * HUD.Scale)),
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


                args.Display.Draw2dRectangle(textSize, Color.Black, 0, System.Drawing.Color.FromArgb(200, 255, 255, 255));

                //args.Display.Draw2dRectangle(new PointF(40f,40f), width, height, 5, Color.Black, 0, Color.FromArgb(220, Color.White));

                args.Display.Draw2dText(Description, Color.Black, anchor + new Rhino.Geometry.Point2d(10, 20),
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


        public class HUD_MouseCallback : Rhino.UI.MouseCallback
        {
            public HUD HUD { get; set; }


            public RhinoViewport ActiveViewport { get; set; }


            public System.Drawing.Point LastMousePoint { get; set; }
            public string LastViewport { get; set; }

            public HUD_MouseCallback() { }
            public HUD_MouseCallback(HUD HUD) { this.HUD = HUD; }


            protected override void OnMouseEnter(MouseCallbackEventArgs e) => ActiveViewport = e.View?.ActiveViewport;
            protected override void OnMouseMove(MouseCallbackEventArgs e)
            {
                if (HUD == null)
                    Enabled = false;

                if (HUD.Component != null)
                {
                    Enabled = !HUD.Component.Locked && !HUD.Component.Hidden;
                }

                HUD.SetHighlightedItem(e.ViewportPoint.X, e.ViewportPoint.Y, e);
                LastMousePoint = e.ViewportPoint;
                Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                base.OnMouseMove(e);
            }


            protected override void OnMouseDown(MouseCallbackEventArgs e)
            {

                if (HUD.HighlightedItem == null)
                {
                    e.Cancel = false;
                    base.OnMouseDown(e);
                    return;
                }
                switch (e.MouseButton)
                {
                    case MouseButton.Left:

                        if (HUD.HighlightedItem.OnLeftClick == null || HUD.HighlightedItem.OnLeftClick.GetInvocationList().Count() == 0)
                        {
                            e.Cancel = false;
                            base.OnMouseDown(e);
                            return;

                        }
                        else
                        {
                            e.Cancel = true;
                            HUD.HighlightedItem.OnLeftClick?.Invoke(this, new HUD_Item.HUD_ItemEventArgs(HUD.HighlightedItem));

                        }
                        break;
                    case MouseButton.Right:


                        e.Cancel = true;
                        ContextMenuStrip menu = new ContextMenuStrip();
                        if (HUD.HighlightedItem?.Box != null)
                        {
                            ToolStripMenuItem menuZoom = new ToolStripMenuItem("Zoom");
                            menuZoom.Click += (s, ee) => HUD.HighlightedItem.ZoomToBox();
                            menu.Items.Add(menuZoom);
                        }


                        foreach (var item in HUD.HighlightedItem.ContextMenuItems)
                        {
                            ToolStripMenuItem menuSelectGH = new ToolStripMenuItem(item.Key);
                            menuSelectGH.Click += (s, ee) => { item.Value(item.Value, new HUD_Item.HUD_ItemEventArgs(HUD.HighlightedItem)); };
                            menu.Items.Add(menuSelectGH);
                        }

                        if (menu.Items.Count > 0)
                        {
                            menu.Show(Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ClientToScreen(e.ViewportPoint));
                        }
                        break;

                    default:
                        break;


                }

                //Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                base.OnMouseDown(e);
            }
        }





    }
}

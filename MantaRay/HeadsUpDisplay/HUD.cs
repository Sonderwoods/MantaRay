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

namespace MantaRay.RadViewer.HeadsUpDisplay
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
        public System.Drawing.Point Anchor { get; set; } = new System.Drawing.Point(50, 40);

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
                if (!ReferenceEquals(_highlightedItem, value))
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








        public class HUD_MouseCallback : MouseCallback
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

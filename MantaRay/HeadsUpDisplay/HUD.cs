using Grasshopper.Kernel;
using MantaRay.Components;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MantaRay.RadViewer.HeadsUpDisplay
{
    public class HUD
    {
        public static Dictionary<Guid, HUD> HUDs = new Dictionary<Guid, HUD>();

        public string Name { get; set; }
        public bool Collapsed = false;
        private bool enabled = true;
        public int Order { get; set; } = 0;
        public static int id = 0;
        public int ID { get; set; } = 0;

        public string FontName { get; set; } = "Arial Rounded MT Bold";
        public string FontDescription { get; set; } = "Arial Unicode MS";

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; UpdateHudPositions(); }
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

        public override string ToString()
        {
            return "HUD: " + Name;
        }

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
                    item?.Draw2D(ref CurrentAnchor, this, args);
                }

            }
            else
            {
                Items.ForEach(item => item.Rectangle = default);
            }

            HighlightedItem?.DrawDescription(this, args);

            CloseBtn?.Draw2D(ref CurrentAnchor, this, args);


        }
        public void UpdateHudPositions()
        {
            if (HUDs.Keys.Contains(Component.InstanceGuid))
            {
                HUDs.Remove(Component.InstanceGuid);
            }
            if (enabled)
                HUDs.Add(Component.InstanceGuid, this);
        }


        public HUD(IGH_Component component) : base()
        {
            Component = component;
            Callback = new HUD_MouseCallback(this);
            CloseBtn = new HUD_CloseButton(this);

        }

        private void SetHighlightedItem(int x, int y, MouseCallbackEventArgs e = null)
        {
            if (e != null && e.View.ActiveViewport.Name != Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Name)
            {
                HighlightedItem = null;
            }
            else
            {
                if (!Collapsed)
                    HighlightedItem = Items.FirstOrDefault(item => item.Rectangle.Contains(x, y));
                else
                    HighlightedItem = null;

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
                if (HUD == null || !HUD.Enabled)
                {
                    Enabled = false;
                    base.OnMouseMove(e);
                    return;
                }

                if (HUD.Component != null)
                {
                    Enabled = !HUD.Component.Locked && !HUD.Component.Hidden;
                }
                if (!Enabled)
                {
                    base.OnMouseMove(e);
                    return;
                }

                var oldHighlighted = HUD.HighlightedItem;

                HUD.SetHighlightedItem(e.ViewportPoint.X, e.ViewportPoint.Y, e);

                LastMousePoint = e.ViewportPoint;

                if (!object.ReferenceEquals(oldHighlighted, HUD.HighlightedItem) || oldHighlighted == null != (HUD.HighlightedItem == null))
                    Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                base.OnMouseMove(e);
            }


            protected override void OnMouseDown(MouseCallbackEventArgs e)
            {


                if (HUD == null || !HUD.Enabled)
                {
                    Enabled = false;
                    e.Cancel = false;
                    base.OnMouseDown(e);
                    return;
                }


                if (!object.ReferenceEquals(((GH_RadViewerSolve)HUD?.Component).hud.Callback, this)) // component is deleted or has got a new HUD..
                {
                    Enabled = false;
                    e.Cancel = false;
                    base.OnMouseDown(e);
                    return;
                }
                switch (e.MouseButton)
                {
                    case MouseButton.Left:

                        if (HUD.HighlightedItem is HUD_CloseButton cb)
                        {
                            e.Cancel = true;
                            //HUD.HighlightedItem.OnLeftClick?.Invoke(this, new HUD_Item.HUD_ItemEventArgs(HUD.HighlightedItem));
                        }
                        else if (HUD.Collapsed || HUD.HighlightedItem == null)
                        {
                            e.Cancel = false;
                            base.OnMouseDown(e);
                            return;
                        }
                        else
                        {
                            e.Cancel = true;
                        }

                        if (HUD.HighlightedItem.OnLeftClick != null && HUD.HighlightedItem.OnLeftClick.GetInvocationList().Count() > 0)
                        {
                            HUD.HighlightedItem.OnLeftClick?.Invoke(this, new HUD_Item.HUD_ItemEventArgs(HUD.HighlightedItem));
                        }
                        break;

                    case MouseButton.Right:


                        e.Cancel = true;
                        ContextMenuStrip menu = new ContextMenuStrip();
                        //if (HUD.HighlightedItem?.Box != null)
                        if (HUD.HighlightedItem.Value.GetBoundingBox() != null)
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

using System.Drawing;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Display;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace MantaRay.RadViewer.HeadsUpDisplay
{

    public class HUD_CloseButton : HUD_Item
    {
        public class HUD_CloseButton_Value : IHasPreview
        {

            public string Name { get; set; } = "X";
            public string Description { get; set; } = "Click to hide\nRight click to remove";
            public void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material, double? transpaceny = null) { }
            public void DrawWires(IGH_PreviewArgs args, int thickness = 1) { }
            public BoundingBox? GetBoundingBox() => null;
            public string GetDescription() => Description;
            public string GetName() => Name;
            public bool HasPreview() => false;
            IEnumerable<GeometryBase> IHasPreview.GetGeometry() => null;

        }

        public HUD_CloseButton(HUD hud) : base(null)
        {
            Value = new HUD_CloseButton_Value();
            HUD = hud;
            Color = Color.FromArgb(200, 255, 255, 255);
            OnLeftClick = new HUD_ContextMenuEventHandler((e, args) =>
            {
                ToggleHideCollapse(hud);
            });

            ContextMenuItems.Add("Remove", new HUD_ContextMenuEventHandler((e, args) =>
            {
                if (HUD != null && HUD.Component != null)
                    HUD.Component.Hidden = true;
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                Grasshopper.Instances.RedrawCanvas();

            }));
            UpdateButtonDescription(hud);



        }

        public void Hide(HUD hud = null)
        {
            HUD _hud = hud ?? HUD ?? null;
            if (_hud != null)
            {
                _hud.Collapsed = true;
                UpdateButtonDescription(hud);
            }
        }

        public void UpdateButtonDescription(HUD hud = null)
        {
            HUD _hud = hud ?? HUD ?? null;
            ((HUD_CloseButton_Value)Value).Name = _hud.Collapsed ? "+" : "X";
            ((HUD_CloseButton_Value)Value).Description = _hud.Name + "\n" + (_hud.Collapsed ? "Click to Expand\nRight click to remove" : "Click to hide\nRight click to remove");
        }

        public void ToggleHideCollapse(HUD hud = null)
        {
            HUD _hud = hud ?? HUD ?? null;
            if (_hud != null && _hud.Enabled)
            {

                _hud.Collapsed = !_hud.Collapsed;
                if (!_hud.Collapsed)
                {
                    foreach (var hu in HUD.HUDs.Where(h => !object.ReferenceEquals(h.Value, this.HUD)))
                    {
                        ((HUD_CloseButton)hu.Value.CloseBtn).Hide();
                    }

                }

                UpdateButtonDescription(hud);

                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
            }
        }
        public int Size { get; set; } = 20;


        public override void Draw2D(ref System.Drawing.Point anchor, HUD HUD, DrawEventArgs args)
        {
            var guids = HUD.HUDs.Keys.OrderBy(x => x).ToArray();
            int order = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                if (HUD.Component.InstanceGuid == guids[i])
                {
                    order = i;
                    break;
                }
            }

            int fontSize = 10;
            bool isItemHighlighted = ReferenceEquals(HUD.HighlightedItem, this);

            Color color = isItemHighlighted ? Color.FromArgb(190, Color) : Color.FromArgb(220, Color);

            Rectangle = new Rectangle(
            x: (int)(HUD.Anchor.X - (Size + 5) * HUD.Scale),
            y: HUD.Anchor.Y + order * (int)(Size * HUD.Scale + 5),
            width: (int)(Size * HUD.Scale),
            height: (int)(Size * HUD.Scale));

            args.Display.Draw2dRectangle(Rectangle, Color.White, isItemHighlighted ? 2 : 0, color);

            args.Display.Draw2dText(
                text: Name,
                color: Color.Black,
                screenCoordinate: new Point2d(HUD.Anchor.X - (5 + Size / 2.0) * HUD.Scale, HUD.Anchor.Y + fontSize * HUD.Scale + order * (int)(Size * HUD.Scale + 5)),
                middleJustified: true,
                height: fontSize,
                fontface: HUD.FontName
            );


            anchor.Y += (int)((HUD.Height + 2) * HUD.Scale);

            //base.Draw(ref anchor, HUD, args);
        }




    }
}

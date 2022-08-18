using GH_IO.Serialization;
using Grasshopper.Kernel;
using MantaRay.RadViewer;
using MantaRay.RadViewer.HeadsUpDisplay;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MantaRay.Components

{

    /// <summary>
    /// Partial class that contains the draw methods and misc grasshopper overrides
    /// </summary>
    public partial class GH_RadViewerSolve : GH_Template
    {
        readonly Random rnd = new Random();

        public void ToggleTwoSided(object s, EventArgs e)
        {
            TwoSided = !TwoSided;

            foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            {
                obj.Material.IsTwoSided = TwoSided;
            }
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

        }

        public void ToggleTransparent(object s, EventArgs e)
        {
            Transparent = !Transparent;

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

        }

        public void ToggleEdges(object s, EventArgs e)
        {
            ShowEdges = !ShowEdges;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void ClearColors(object s, EventArgs e)
        {
            colors.Clear();
            AssignColors();
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void ToggleColors(object s, EventArgs e)
        {
            colors.Clear();
            Polychromatic = !Polychromatic;
            ExpireSolution(true);
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }


        void SetupHUD()
        {
            //Make sure we're on.
            hud.Component = this;
            hud.Callback.Enabled = true;

            //Set up close button
            hud.CloseBtn.ContextMenuItems.Clear();
            hud.CloseBtn.ContextMenuItems.Add("Toggle Twosided", ToggleTwoSided);
            hud.CloseBtn.ContextMenuItems.Add("Transparent", ToggleTransparent);
            hud.CloseBtn.ContextMenuItems.Add("Toggle Edges", ToggleEdges);
            hud.CloseBtn.ContextMenuItems.Add("Update Colors", ClearColors);
            hud.CloseBtn.ContextMenuItems.Add("Toggle Colors", ToggleColors);

            //Set up all items
            hud.Items.Clear();
            foreach (var obj in objects.Values)
            {
                hud.Items.Add(new HUD_Item(obj));
            }

            AssignColors();

        }

        public void AssignColors()
        {
            foreach (var item in hud.Items)
            {
                if (!colors.ContainsKey(item.Name))
                {
                    colors.Add(item.Name, Color.FromArgb(rnd.Next(100, 255), rnd.Next(100, 255), rnd.Next(100,255)));
                }
                item.Color = colors[item.Name];
            }
        }

        /// <summary>
        /// Our actual 2D Draw method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawForeground(object sender, DrawEventArgs e)
        {

            if (hud != null && hud.Enabled && !this.Hidden && !this.Locked)
            {

                hud.Draw(e);
                hud.Callback.Enabled = true;

            }
            else if (hud != null)
            {

                hud.Enabled = false;

            }

        }

        #region GHoverrides


        //public override TimeSpan ProcessorTime => timeSpan;

        public override BoundingBox ClippingBox => bb ?? new BoundingBox(new[] { new Point3d(0, 0, 0) });

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (this.Locked) return;

            if (hud != null && hud.Items.Count > 0)
            {
                //ensures that we turn the dashboard on
                hud.Enabled = true;
                hud.Callback.Enabled = true;
            }

            // STANDARD PROPERTIES
            DisplayMaterial material = new DisplayMaterial()
            {
                Transparency = Transparent ? 0.5 : 0.0,
                Emission = Polychromatic ? hud?.HighlightedItem?.Color ?? Color.LightGray : Color.LightGray,
                IsTwoSided = TwoSided,
                BackDiffuse = Color.DarkRed,
                BackEmission = Color.DarkRed,
                BackTransparency = TwoSided ? 0.0 : 0.9

            };

            Color? nullColor = null;

            if (hud.HighlightedItem != null && !(hud.HighlightedItem is HUD_CloseButton))
            {

                foreach (var item in hud.Items.Where(i => !ReferenceEquals(i, hud.HighlightedItem)))
                    item.DrawMesh(args, material, Polychromatic ? nullColor : Color.LightGray);


                // PROPERTIES OVERRIDES FOR THE SELECTED ITEM
                material.BackDiffuse = Color.Red;
                material.BackEmission = Color.Red;
                material.Transparency = 0.0;

                hud.HighlightedItem.DrawMesh(args, material);

            }
            else
            {
                foreach (var item in hud.Items)
                    item.DrawMesh(args, material, Polychromatic ? nullColor : Color.LightGray);
            }

            base.DrawViewportMeshes(args);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (!this.Locked)
            {
                if (hud.HighlightedItem != null && !hud.HighlightedItem.GetType().IsSubclassOf(typeof(HUD_Item)))
                {
                    hud.HighlightedItem.DrawEdges(args);

                }
                else if (ShowEdges)
                {
                    foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
                    {
                        obj.DrawWires(args);
                    }
                }
            }

            foreach (Curve crv in failedCurves)
            {
                args.Display.DrawCurve(crv, System.Drawing.Color.Red);
            }

            base.DrawViewportWires(args);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("IsTwoSided", ref TwoSided);
            reader.TryGetBoolean("Polychromatic", ref Polychromatic);
            reader.TryGetBoolean("ShowEdges", ref ShowEdges);
            reader.TryGetBoolean("Transparent", ref Transparent);
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("IsTwoSided", TwoSided);
            writer.SetBoolean("Polychromatic", Polychromatic);
            writer.SetBoolean("ShowEdges", ShowEdges);
            writer.SetBoolean("Transparent", Transparent);
            return base.Write(writer);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Toggle Twosided", ToggleTwoSided, true, TwoSided);
            Menu_AppendItem(menu, "Transparent", ToggleTransparent, true, Transparent);
            Menu_AppendItem(menu, "Show edges", ToggleEdges, true, ShowEdges);
            Menu_AppendItem(menu, "Use Colors", ToggleColors, true, Polychromatic);
            Menu_AppendItem(menu, "Clear Colors", ClearColors, true);
        }


        //protected override void ExpireDownStreamObjects()
        //{
        //    if (!isRunning)
        //        base.ExpireDownStreamObjects();
        //}

        protected override void BeforeSolveInstance()
        {
            Debug.WriteLine("Beforesolveinstance ran. Setting up events");
            DisplayPipeline.DrawForeground += DrawForeground;
        }


        public override void RemovedFromDocument(GH_Document document)
        {

            Debug.WriteLine("Removed from document and set to false/null");

            base.RemovedFromDocument(document);

            hud.Callback.Enabled = false;

            hud = null;

            DisplayPipeline.DrawForeground -= DrawForeground;

        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {

            if (hud != null)
            {

                DisplayPipeline.DrawForeground -= DrawForeground;
                if (context == GH_DocumentContext.Loaded)
                {
                    Debug.WriteLine("DocumentContext changed, enabled set to true");
                    DisplayPipeline.DrawForeground += DrawForeground;
                    hud.Enabled = true;

                }
                else
                {
                    Debug.WriteLine("DocumentContext changed, enabled set to false");
                    hud.Enabled = false;
                }
            }

        }


        public override bool IsPreviewCapable => true;

        public override Guid ComponentGuid => new Guid("1FA443D0-8881-4546-9BA1-259B22CF89B4");

        protected override Bitmap Icon => Resources.Resources.Ra_Radviewer_Icon;


        #endregion


    }
}

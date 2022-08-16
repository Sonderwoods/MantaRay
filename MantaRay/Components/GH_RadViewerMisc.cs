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
    public partial class GH_RadViewerSolve : GH_Template
    {




        public void ToggleTwoSided(object s, EventArgs e)
        {
            TwoSided = !TwoSided;

            foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            {
                obj.Material.IsTwoSided = TwoSided;
            }

        }

        public void ToggleTransparent(object s, EventArgs e)
        {
            Transparent = !Transparent;

            foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            {
                obj.Material.Transparency = Transparent ? 0.3 : 0.0;
            }


        }




        

        public void ClearColors(object s, EventArgs e)
        {
            colors.Clear();
            ExpireSolution(true);
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
            hud.Component = this;
            hud.Callback.Enabled = true;

            hud.Items.Clear();

            if (!hud.CloseBtn.ContextMenuItems.ContainsKey("Toggle Twosided"))
            {
                hud.CloseBtn.ContextMenuItems.Add("Toggle Twosided", ToggleTwoSided);
            }
            if (!hud.CloseBtn.ContextMenuItems.ContainsKey("Transparent"))
            {
                hud.CloseBtn.ContextMenuItems.Add("Transparent", (s, e) => { Transparent = !Transparent; });
            }
            if (!hud.CloseBtn.ContextMenuItems.ContainsKey("Toggle Edges"))
            {
                hud.CloseBtn.ContextMenuItems.Add("Toggle Edges", (s, e) => { ShowEdges = !ShowEdges; });
            }
            if (!hud.CloseBtn.ContextMenuItems.ContainsKey("Update Colors"))
            {
                hud.CloseBtn.ContextMenuItems.Add("Update Colors", ClearColors);
            }
            if (!hud.CloseBtn.ContextMenuItems.ContainsKey("Toggle Colors"))
            {
                hud.CloseBtn.ContextMenuItems.Add("Toggle Colors", ToggleColors);
            }



            foreach (var obj in objects)
            {

                hud.Items.Add(new HUD_Item(obj.Value));
                //try
                //{

                //    //string desc = poly.Modifier is RadianceMaterial m ? m.MaterialDefinition : string.Empty;
                //    //hud.Items.Add(new HUD_Item() { Name = poly.Name, Description = desc, Mesh = poly.Mesh, Color = poly.Material.Diffuse });
                    

                //}
                //catch (Exception e)
                //{
                //    ErrorMsgs.Add("add hud: " + e.Message);
                //}
            }

            
        }


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


        public override TimeSpan ProcessorTime => timeSpan;



        public override BoundingBox ClippingBox => bb.Value;

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {


            if (this.Locked)
                return;

            if (hud != null && hud.Items.Count > 0)
            {
                //ensures that we turn the dashboard on
                hud.Enabled = true;
                hud.Callback.Enabled = true;
            }
            //if (hud != null && hud.Items.Count > 0 && !TwoSided)
            //if (hud != null && hud.Items.Count > 0)
            //if (true)
            //{

            if (hud.HighlightedItem != null && !hud.HighlightedItem.GetType().IsSubclassOf(typeof(HUD_Item)))
            {
                hud.HighlightedItem.DrawMesh(args, 1, TwoSided);

                foreach (var item in hud.Items.Where(i => !object.ReferenceEquals(i, hud.HighlightedItem)))
                    item.DrawMesh(args, 0.2, grey: true);

            }
            else
            {
                //foreach (var item in hud.Items)
                //    item.DrawMesh(args, Transparent ? 0.9 : 1.0);

                foreach (KeyValuePair<string, RadianceObjectCollection> pair in objects)
                {
                    pair.Value.DrawPreview(args, pair.Value.Material);
                    //obj.DrawObject(args, Transparent ? 0.9 : 1.0); //This one works with twosided option.
                }
            }
            //}
            //else
            //{
            //    foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            //    {
            //        obj.DrawObject(args, Transparent ? 0.9 : 1.0); //This one works with twosided option.
            //    }
            //}



            //foreach( Curve crv in failedCurves)
            //{
            //    args.Display.DrawCurve(crv, System.Drawing.Color.Red);
            //}

            base.DrawViewportMeshes(args);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (!this.Locked)
            {
                if (hud.HighlightedItem != null && !hud.HighlightedItem.GetType().IsSubclassOf(typeof(HUD_Item)))
                {
                    hud.HighlightedItem.DrawEdges(args);

                    //foreach (var item in hud.Items.Where(i => !object.ReferenceEquals(i, hud.HighlightedItem)))
                    //    item.DrawEdges(args);

                }
                else if (ShowEdges)
                {
                    foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
                    {
                        obj.DrawWires(args);
                    }
                }
            }

            //foreach( Curve crv in failedCurves)
            //{
            //    args.Display.DrawCurve(crv, System.Drawing.Color.Red);
            //}

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
            Menu_AppendItem(menu, "Show edges", (s, e) => { ShowEdges = !ShowEdges; }, true, ShowEdges);
            Menu_AppendItem(menu, "Use Colors", ToggleColors, true, Polychromatic);
            Menu_AppendItem(menu, "Clear Colors", ClearColors, true);
        }


        protected override void ExpireDownStreamObjects()
        {
            if (!isRunning)
                base.ExpireDownStreamObjects();
        }

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

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_ParseRhinoFile : GH_Template, IHasDoubleClick
    {
        /// <summary>
        /// Initializes a new instance of the GH_ParseRhinoFile class.
        /// </summary>
        public GH_ParseRhinoFile()
          : base("ParseRhinoFile", "ParseRhinoFile",
              "Reads rhino layers, gets geometry and prepares materials for radiance\n\nDouble click me to recompute",
              "1 Setup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("LayerPrefix", "Prefix", "Layer prefix.\nUse the default to search for all layers\n" +
                "that are sublayers to 'Daylight_Input' and starts with '_'.", GH_ParamAccess.item, "Daylight_Input::_")].Optional = true;
            pManager[pManager.AddTextParameter("Grids", "Grids", "Grids", GH_ParamAccess.item, "_Grids")].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("GlassGeometries", "GlassGeometries", "Geometries", GH_ParamAccess.list);
            pManager.AddTextParameter("GlassModifiers", "GlassModifiers", "Modifiers", GH_ParamAccess.list);
            pManager.AddGenericParameter("-", "-", "-", GH_ParamAccess.item);

            pManager.AddMeshParameter("OpaqueGeometries", "OpaqueGeometries", "Geometries", GH_ParamAccess.list);
            pManager.AddTextParameter("OpaqueModifiers", "OpaqueModifiers", "Modifiers", GH_ParamAccess.list);
            pManager.AddGenericParameter("-", "-", "-", GH_ParamAccess.item);

            pManager.AddMeshParameter("CustomGeometries", "CustomGeometries", "Geometries", GH_ParamAccess.list);
            pManager.AddTextParameter("ModifierNames", "ModifierNames", "ModifierNames", GH_ParamAccess.list);
            pManager.AddGenericParameter("-", "-", "-", GH_ParamAccess.item);

            pManager.AddBrepParameter("Grids", "Grids", "Grids", GH_ParamAccess.list);
            pManager.AddTextParameter("GridNames", "GridNames", "GridNames", GH_ParamAccess.list);
            pManager.AddGenericParameter("-", "-", "-", GH_ParamAccess.item);

            pManager.AddTextParameter("SkippedLayers", "SkippedLayers", "SkippedLayers", GH_ParamAccess.list);


        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Mesh> glassGeometries = new List<Mesh>();
            List<Radiance.Material> glassMaterials = new List<Radiance.Material>();

            List<Mesh> opaqueGeometries = new List<Mesh>();
            List<Radiance.Material> opaqueMaterials = new List<Radiance.Material>();

            List<Mesh> customGeometries = new List<Mesh>();
            List<string> customModifierNames = new List<string>();

            List<string> missingLayers = new List<string>();

            List<Brep> grids = new List<Brep>();
            List<string> gridNames = new List<string>();


            string prefix = DA.Fetch<string>(this, "LayerPrefix");
            string gridLayer = DA.Fetch<string>(this, "Grids");

            foreach (var layer in Rhino.RhinoDoc.ActiveDoc.Layers.Where(l => l.FullPath.StartsWith(prefix)))
            {


                if (Rhino.RhinoDoc.ActiveDoc.Objects.Count(o => o.Attributes.LayerIndex == layer.Index) > 0)
                {


                    string[] ln = layer.FullPath.Substring(prefix.Length).Split('_');

                    if (ln.Length == 1 && string.Equals(ln[0], gridLayer, StringComparison.InvariantCulture))
                    {



                        foreach (var obj in Rhino.RhinoDoc.ActiveDoc.Objects
                        .Where(o => o.Attributes.LayerIndex == layer.Index)
                        .Select(o => o))
                        {
                            string _name = obj.Attributes.Name;
                            switch (obj.Geometry.Duplicate())
                            {
                                case Curve curve:
                                    if (curve.IsClosed)
                                    {
                                        grids.Add(InputGeometryHelper.UpwardsPointingBrepsFromCurves(new List<Curve> { curve })[0]);
                                        gridNames.Add(_name);
                                    }
                                    break;
                                case Brep brep:
                                    brep.TurnUp();
                                    grids.Add(brep);
                                    gridNames.Add(_name);

                                    break;
                                case Mesh mesh:
                                    throw new NotImplementedException("No meshes as grids. yet");

                                case Surface surface:
                                    Brep b = Brep.CreateFromSurface(surface);
                                    b.TurnUp();
                                    grids.Add(b);
                                    gridNames.Add(_name);

                                    break;
                            }
                        }
                    }
                    else if (ln.Length != 2)
                    {
                        missingLayers.Add(layer.FullPath);
                        continue;
                    }


                    Mesh m = new Mesh();

                    foreach (var obj in Rhino.RhinoDoc.ActiveDoc.Objects
                        .Where(o => o.Attributes.LayerIndex == layer.Index)
                        .Select(o => o.Geometry))
                    {
                        switch (obj)
                        {
                            case Brep brep:
                                m.Append(Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh));
                                break;
                            case Mesh mesh:
                                m.Append(mesh);
                                break;
                            case Surface surface:
                                m.Append(Mesh.CreateFromSurface(surface, MeshingParameters.FastRenderMesh));
                                break;

                        }
                    }
                    if (m.Faces.Count == 0) continue;


                    string name = StringHelper.ToSafeName(ln[0]);
                    string layerSuffix = ln[1];


                    if (layerSuffix[layerSuffix.Length - 1] == '%'
                        && double.TryParse(layerSuffix.Substring(0, layerSuffix.Length - 1), out double transmittance)) // we have a glass
                    {
                        glassGeometries.Add(m);
                        glassMaterials.Add(Radiance.Material.CreateGlassFromTransmittance(name, transmittance, out _));
                    }

                    else if (double.TryParse(layerSuffix, out double reflectance))
                    {
                        opaqueGeometries.Add(m);
                        opaqueMaterials.Add(Radiance.Material.CreateOpaqueFromReflection(name, reflectance, out _));
                    }
                    else
                    {
                        customGeometries.Add(m);
                        customModifierNames.Add(name);
                    }
                }
            }

            DA.SetDataList("GlassGeometries", glassGeometries);
            DA.SetDataList("GlassModifiers", glassMaterials.Select(m => m.ToString()));

            DA.SetDataList("OpaqueGeometries", opaqueGeometries);
            DA.SetDataList("OpaqueModifiers", opaqueMaterials.Select(m => m.ToString()));


            DA.SetDataList("CustomGeometries", customGeometries);
            DA.SetDataList("ModifierNames", customModifierNames);

            DA.SetDataList("Grids", grids);
            DA.SetDataList("GridNames", gridNames);

            DA.SetDataList("SkippedLayers", missingLayers);


        }

        public override void CreateAttributes()
        {
            m_attributes = new GH_DoubleClickAttributes(this);

        }

        public GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            this.ExpireSolution(true);
            return GH_ObjectResponse.Handled;
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2791A6C7-3161-48E9-B647-32B952C7B9FD"); }
        }
    }
}
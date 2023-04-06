using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
              "Reads rhino layers, gets geometry and prepares materials for radiance\n\n" +
                "Double click me to recompute\n\n\n" +
                "The layer table in Rhino (by default) could look like the below:\n" +
                "'Daylight_Inputs' (with the below sublayers)\n" +
                "- '_Floors_20' (will get reflectance of 0.2)\n" +
                "- '_MyWindows_70%' (will become glass with the transmittance of 0.7, due to the '%' suffix\n" +
                "- '_Grids' (will output grid geometries for points)",
              "1 Setup")
        {
        }

        List<Brep> _grids = new List<Brep>();

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("LayerPrefix", "Prefix", "Layer prefix.\nUse the default to search for all layers\n" +
                "that are sublayers to 'Daylight_Input' and starts with '_'.", GH_ParamAccess.item, "Daylight_Inputs::_")].Optional = true;
            pManager[pManager.AddTextParameter("Grids", "Grids", "Grids", GH_ParamAccess.item, "Grids")].Optional = true;
            pManager[pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false)].Optional = true;
        }

        /// <summary>
        /// Grid geometries param index
        /// </summary>
        int gg = 0;
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

            gg = pManager.AddBrepParameter("GridGeometries", "GridGeometries", "Grids", GH_ParamAccess.list);
            pManager.HideParameter(gg);

            pManager.AddTextParameter("GridNames", "GridNames", "GridNames", GH_ParamAccess.list);
            pManager.AddGenericParameter("-", "-", "-", GH_ParamAccess.item);

            pManager.AddTextParameter("SkippedLayers", "SkippedLayers", "SkippedLayers", GH_ParamAccess.list);

            pManager.AddBooleanParameter("Ran", "Ran", "Ran without any err", GH_ParamAccess.tree); //always keep ran as the last parameter


        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TimingHelper th = new TimingHelper("ParseRhinoFile");
            _grids.Clear();

            if(!DA.Fetch<bool>(this, "Run"))
            {
                return;
            }

            List<Mesh> glassGeometries = new List<Mesh>();
            List<Radiance.Material> glassMaterials = new List<Radiance.Material>();

            List<Mesh> opaqueGeometries = new List<Mesh>();
            List<Radiance.Material> opaqueMaterials = new List<Radiance.Material>();

            List<Mesh> customGeometries = new List<Mesh>();
            List<string> customModifierNames = new List<string>();

            List<string> missingLayers = new List<string>();

            List<IGH_GeometricGoo> grids = new List<IGH_GeometricGoo>();
            List<string> gridNames = new List<string>();


            string prefix = DA.Fetch<string>(this, "LayerPrefix");
            string gridLayer = DA.Fetch<string>(this, "Grids");

            object opaqueLock = new object();
            object glassLock = new object();
            object customLock = new object();

            //Parallel.ForEach<Layer>(Rhino.RhinoDoc.ActiveDoc.Layers.Where(l => l.FullPath.StartsWith(prefix)), (layer) =>
            //{


            th.Benchmark("Prestuff");







            Parallel.ForEach<Layer>(Rhino.RhinoDoc.ActiveDoc.Layers.Where(l => l.FullPath.StartsWith(prefix)), (layer) =>
            {


                //    foreach (Layer layer in Rhino.RhinoDoc.ActiveDoc.Layers.Where(l => l.FullPath.StartsWith(prefix)).AsParallel())
                //{
                IEnumerable<RhinoObject> objs = Rhino.RhinoDoc.ActiveDoc.Objects.GetObjectList(new ObjectEnumeratorSettings()
                {
                    ActiveObjects = true,
                    LockedObjects = true,
                    HiddenObjects = true,
                    IncludeGrips = false,
                    IncludeLights = false,
                    IncludePhantoms = false,
                    ReferenceObjects = true,
                    IdefObjects = false,
                    ObjectTypeFilter = ObjectType.Brep | ObjectType.Mesh | ObjectType.Surface | ObjectType.Extrusion,
                    NormalObjects = true,
                    LayerIndexFilter = layer.Index
                });

                string[] ln = layer.FullPath.Substring(prefix.Length).Split('_');

                if (ln.Length == 1 && string.Equals(ln[0], gridLayer, StringComparison.InvariantCulture))
                {

                    // Check for curves:


                    foreach (var obj in objs)
                    {
                        string _name = obj.Attributes.Name ?? string.Empty;
                        switch (obj.Geometry)
                        {
                            case Curve curve:
                                if (curve.IsClosed)
                                {
                                    grids.Add(new GH_Brep(InputGeometryHelper.UpwardsPointingBrepsFromCurves(new List<Curve> { curve.DuplicateCurve() })[0]));
                                    gridNames.Add(_name);

                                }
                                break;
                            case Brep brep:
                                brep.TurnUp();
                                grids.Add(new GH_Brep(brep.DuplicateBrep()));
                                gridNames.Add(_name);

                                break;
                            case Mesh mesh:
                                grids.Add(new GH_Mesh(mesh.DuplicateMesh()));
                                break;

                            case Surface surface:
                                Brep b = Brep.CreateFromSurface((Surface)surface.Duplicate());
                                b.TurnUp();
                                grids.Add(new GH_Brep(b));
                                gridNames.Add(_name);

                                break;
                        }
                    }
                    //continue;
                    return;


                }
                else if (ln.Length != 2)
                {
                    missingLayers.Add(layer.FullPath);
                    //continue;
                    return;
                }


                Mesh m = new Mesh();


                foreach (var obj in objs)
                {

                    switch (obj.Geometry)
                    {
                        case Brep brep:
                            int count = m.Vertices.Count;
                            m.Append(Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh));

                            if (m.Vertices.Count == count) //didnt add anything, maybe the brep wasnt loaded.
                            {
                                m.Append(Mesh.CreateFromBrep(brep, MeshingParameters.Default));
                            }
                            break;
                        case Mesh mesh:
                            m.Append(mesh);
                            break;
                        case Surface surface:
                            int count2 = m.Vertices.Count;
                            m.Append(Mesh.CreateFromSurface(surface, MeshingParameters.FastRenderMesh));
                            if (m.Vertices.Count == count2) //didnt add anything, maybe the brep wasnt loaded.
                            {
                                m.Append(Mesh.CreateFromSurface(surface, MeshingParameters.Default));
                            }
                            break;

                    }
                }
                if (m.Faces.Count == 0)
                {
                    missingLayers.Add(layer.FullPath);
                    return;

                }


                string layerSuffix = ln[1];


                if (layerSuffix[layerSuffix.Length - 1] == '%'
                    && double.TryParse(layerSuffix.Substring(0, layerSuffix.Length - 1), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double transmittance)) // we have a glass
                {
                    lock (glassLock)
                    {
                        glassGeometries.Add(m);
                        glassMaterials.Add(Radiance.Material.CreateGlassFromTransmittance($"{ln[0]}_{transmittance}", transmittance, out _));
                    }
                }

                else if (double.TryParse(layerSuffix, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double reflectance))
                {
                    lock (opaqueLock)
                    {
                        opaqueGeometries.Add(m);
                        opaqueMaterials.Add(Radiance.Material.CreateOpaqueFromReflection($"{ln[0]}_{reflectance:0.00}", reflectance, out _));
                    }
                }
                else
                {
                    lock (customLock)
                    {
                        customGeometries.Add(m);
                        customModifierNames.Add(ln[1]);
                    }
                }

            }); // end loop
            th.Benchmark("Parsed layers in parallel");









            DA.SetDataList("GlassGeometries", glassGeometries);
            DA.SetDataList("GlassModifiers", glassMaterials.Select(m => m.ToString()));

            DA.SetDataList("OpaqueGeometries", opaqueGeometries);
            DA.SetDataList("OpaqueModifiers", opaqueMaterials.Select(m => m.ToString()));


            DA.SetDataList("CustomGeometries", customGeometries);
            DA.SetDataList("ModifierNames", customModifierNames);

            DA.SetDataList(gg, grids);
            DA.SetDataList("GridNames", gridNames);

            DA.SetDataList("SkippedLayers", missingLayers);

            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);


        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            foreach (var item in _grids)
            {
                args.Display.DrawBrepShaded(item, new Rhino.Display.DisplayMaterial(System.Drawing.Color.Blue));

            }
            base.DrawViewportMeshes(args);

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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay
{
    /// <summary>
    /// Original (C) Henning Larsen Architects 2019
    /// GPL License
    /// Author: Mathias Sønderskov
    /// https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public
    /// </summary>
    public class GhGridViewer : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GhGridViewer class.
        /// </summary>
        public GhGridViewer()
          : base("GridViewer", "GridViewer",
              "Gridviewer\nBased on https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public",
              "2 Radiance")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary; // puts the grid component in top.

        int i_inputGradient, i_inputSelectorMin, i_inputSelecterMax, i_inputSelectorSectionType = 0;

        //ToolStripMenuItem menuItemCaps = new ToolStripMenuItem();

        //private bool caps = true;

        //public bool Caps
        //{
        //    get { return caps; }
        //    set
        //    {
        //        caps = value;
        //        if ((caps))
        //        {
        //            Message = "Set Caps Off";
        //        }
        //        else
        //        {
        //            Message = "Set Caps On";
        //        }
        //    }
        //}


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //0
            pManager.AddGenericParameter("Grids", "Grids", "Grids", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;

            //1
            pManager.AddNumberParameter("Results", "Results", "Results", GH_ParamAccess.tree);

            //2
            i_inputGradient = pManager.AddColourParameter("_Gradient", "_Gradient", "Gradient.\nPro tip:You can set the min and max in the gradient component! :-)", GH_ParamAccess.item);
            pManager[i_inputGradient].Optional = true;

            ////3
            //var inputRange = pManager.AddTextParameter("GradientRange", "GradientRange*", "Range of the colors", GH_ParamAccess.item, String.Empty);
            //pManager[inputRange].Optional = true;

            //4
            //pManager.AddBooleanParameter("Cap", "Cap*", "Cap min and max?\nBUGGY ON THE ISOCURVES. LEAVE DEFAULT.", GH_ParamAccess.item, false);



            //5
            i_inputSelectorMin = pManager.AddColourParameter("_MinColor", "_MinColor", "MinColor input to cap colors below the gradient max", GH_ParamAccess.item, Color.DarkGray);
            pManager[i_inputSelectorMin].Optional = true;

            //6
            i_inputSelecterMax = pManager.AddColourParameter("_MaxColor", "_MaxColor", "MaxColor input to cap colors above the gradient max", GH_ParamAccess.item, Color.LightGray);
            pManager[i_inputSelecterMax].Optional = true;

            //7
            i_inputSelectorSectionType = pManager.AddGenericParameter("_Section Type", "_Section Type", "Connect a section type component", GH_ParamAccess.item);
            pManager[i_inputSelectorSectionType].Optional = true;



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            int cm = pManager.AddMeshParameter("coloredMesh", "coloredMesh", "mesh", GH_ParamAccess.list);
            pManager.HideParameter(cm);


            pManager.AddMeshParameter("layeredMesh", "layeredMesh", "mesh", GH_ParamAccess.tree);


            int c = pManager.AddCurveParameter("curves", "curves", "planes", GH_ParamAccess.tree);
            pManager.HideParameter(c);

            int p = pManager.AddPlaneParameter("Planes", "P", "planes", GH_ParamAccess.list);
            pManager.HideParameter(p);

            int m = pManager.AddMeshParameter("TempMeshes", "TM", "planes", GH_ParamAccess.list);
            pManager.HideParameter(m);

            int co = pManager.AddColourParameter("Colors", "Colors", "Colors", GH_ParamAccess.tree);
            pManager[co].DataMapping = GH_DataMapping.Flatten;

            int va = pManager.AddTextParameter("Values", "Values", "Values", GH_ParamAccess.tree);
            pManager[va].DataMapping = GH_DataMapping.Flatten;


        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            #region updateInputs
            //if (!cap && this.Params.Input.Count ==7)
            //{
            //    this.Params.Input[5].RemoveAllSources();
            //    this.Params.UnregisterInputParameter(this.Params.Input[5]);
            //    this.Params.Input[6].RemoveAllSources();
            //    this.Params.UnregisterInputParameter(this.Params.Input[6]);

            //    Params.OnParametersChanged();
            //}
            //if (cap && this.Params.Input.Count == 5)
            //{
            //    this.Params.RegisterInputParam(new Param_Colour
            //    {
            //        Name = "MinColor",
            //        NickName = "MinColor",
            //        Description = "MinColor",
            //        Access = GH_ParamAccess.item,
            //        Optional = true
            //    });
            //    this.Params.RegisterInputParam(new Param_Colour
            //    {
            //        Name = "MaxColor",
            //        NickName = "MaxColor",
            //        Description = "MinColor",
            //        Access = GH_ParamAccess.item,
            //        Optional = true
            //    });

            //    Params.OnParametersChanged();
            //}

            #endregion updateInputs

            //bool caps = DA.Fetch<bool>("Cap");
            Color? maxColor = DA.Fetch<Color?>(this, i_inputSelecterMax);
            Color? minColor = DA.Fetch<Color?>(this, i_inputSelectorMin);
            var allResults = DA.FetchTree<GH_Number>(this, "Results");
            var grids = DA.FetchList<Grid>(this, "Grids");
            //var gradientRange = DA.Fetch<string>("GradientRange");
            //int maxCount = DA.Fetch<int>("MaxCount");
            int maxCount = 200;
            //var inStepSize = DA.Fetch<int>("StepSize");
            //var inSteps = DA.Fetch<int>("Steps");
            GridTypeSelector inputSelector = DA.Fetch<GridTypeSelector>(this, "_Section Type");

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int g = 0; g < grids.Count; g++)
            {
                globalMin = Math.Min(globalMin, ((List<GH_Number>)allResults.get_Branch(g)).Select(r => r.Value).Min());
                globalMax = Math.Max(globalMax, ((List<GH_Number>)allResults.get_Branch(g)).Select(r => r.Value).Max());
            }


            if (inputSelector == null)
            {
                inputSelector = new GridTypeSelector(10, globalMin, globalMax);
            }



            if (allResults.Branches.Count != grids.Count)
                throw new Exception("Grid count doesnt match results");


            //var colorDomain = Misc.AutoDomain(gradientRange, allResults);
            //Rhino.RhinoApp.WriteLine($"{range}  ->  {domain[0]} to {domain[1]}");

            GH_GradientControl gc;
            try
            {
                gc = (GH_GradientControl)Params.Input[i_inputGradient].Sources[0].Attributes.GetTopLevel.DocObject;

            }
            catch (System.ArgumentOutOfRangeException)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Remember to add a gradient component in grasshopper to change colors!");
                gc = null;


            }

            GradientParser gp = new GradientParser(gc)
            {
                //Cap = caps,
                AboveMax = maxColor == default(Color) ? null : maxColor,
                BelowMin = minColor == default(Color) ? null : minColor,
                Min = inputSelector.Min,
                Max = inputSelector.Max,
                Reverse = Params.Input[i_inputGradient].Reverse
            };





            IDictionary<string, Color> colorDescriptions = new Dictionary<string, Color>();
            IDictionary<string, int> colorPaths = new Dictionary<string, int>();





            #region coloredMesh
            var outMeshes = new List<Mesh>();



            for (int i = 0; i < grids.Count; i++)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Mesh vertices: {grids[i].SimMesh.Vertices.Count}, colors = {gp.GetColors(allResults.Branches[i].Select(p => p.Value).ToArray()).Length} f");


                outMeshes.Add(grids[i].GetColoredMesh(gp.GetColors(allResults.Branches[i].Select(p => p.Value).ToArray())));

                Mesh m = grids[i].SimMesh;
                Point3d[] points = grids[i].SimPoints.ToArray();
                outMeshes[outMeshes.Count - 1].Translate(0, 0, UnitHelper.FromMeter(0.001));
            }


            DA.SetDataList(0, outMeshes);

            #endregion coloredMesh



            #region layeredMesh

            if (grids[0].UseCenters == true)
            {

                return;

            }

            //Outputs
            GH_Structure<GH_Mesh> oLayeredMeshes = new GH_Structure<GH_Mesh>();
            List<GH_Mesh> previewMeshes = new List<GH_Mesh>();
            List<GH_Plane> outPlanes = new List<GH_Plane>();
            GH_Structure<GH_Curve> outCurves = new GH_Structure<GH_Curve>();

            GH_Structure<GH_String> outValues = new GH_Structure<GH_String>();
            GH_Structure<GH_Colour> outColors = new GH_Structure<GH_Colour>();

            const double SCALAR = 1; // don't change.



            //if (gc != null && ((GH_Structure<GH_Number>)gc.Params.Input[1].VolatileData)[0][0].Value == 1)
            //{
            //    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The gradient connected has 1 as max. Is that on purpose? Check the inputs of your gradient component." +
            //        $"\nI suggest you set your max somewhere around {globalMax:0.0}");
            //}


            for (int g = 0; g < grids.Count; g++)
            {

                //GH_Structure<GH_Curve> curves = new GH_Structure<GH_Curve>();
                Grid grid = grids[g];
                Mesh inputMesh = grids[g].SimMesh.DuplicateMesh();
                //Mesh meshToCut = grids[g].SimMesh;

                List<double> results = ((List<GH_Number>)allResults.get_Branch(g)).Select(r => r.Value).ToList();

                if (grids[g].UseCenters == true)
                {
                    results = Helpers.RTreeHelper.FindClosestWeightedValues(grids[g], results, true).ToList();
                    // ADD CONVERSION TODO:
                }

                inputMesh.Normals.ComputeNormals();

                Vector3f normal = inputMesh.FaceNormals[0];

                Plane basePlane = new Plane(inputMesh.Vertices[0], normal);

                Transform ProjectToBase = Transform.PlanarProjection(basePlane);

                Plane cuttingPlane = new Plane(basePlane);

                Mesh meshToCut = CreateMeshToBeCut(SCALAR, inputMesh, results, cuttingPlane);

                previewMeshes.Add(new GH_Mesh(inputMesh));

                MeshingParameters mp = MeshingParameters.FastRenderMesh;

                List<Mesh> layeredMeshesThisGrid = new List<Mesh>();


                double valueForSmallAreas = double.MinValue;

                double resultsMin = results.Min();

                foreach (var item in inputSelector)
                {
                    if (resultsMin >= item)
                    {
                        valueForSmallAreas = item;
                        break;
                    }
                }

                //Color col = gp.GetColors(new List<double>() { inputSelector.Min.Value })[0];
                Color col = gp.GetColors(new List<double>() { gp.BelowMin.HasValue && inputSelector.Min.Value <= gp.Min ? resultsMin > gp.Min ? valueForSmallAreas : double.MinValue : inputSelector.Min.Value })[0];

                Polyline[] outlinePolylines = inputMesh.GetNakedEdges();

                PolylineCurve[] curvesFromOutline = new PolylineCurve[outlinePolylines.Length];

                for (int i = 0; i < outlinePolylines.Length; i++)
                {
                    curvesFromOutline[i] = new PolylineCurve(outlinePolylines[i]);
                    curvesFromOutline[i].Transform(ProjectToBase);
                }


                Mesh meshFromCurves = GetMeshFromCurves(curvesFromOutline, mp, in col);

                GH_Path startPath = new GH_Path(g, -1);
                oLayeredMeshes.Append(new GH_Mesh(meshFromCurves), startPath);


                string lessThanKey = gp.BelowMin.HasValue && inputSelector.Min.Value < gp.Min ? $"<{gp.Min:0.0}" : $"<{inputSelector.Min.Value:0.0}";
                if (!colorDescriptions.ContainsKey(lessThanKey) && inputSelector.First() < gp.Min)
                {
                    colorDescriptions.Add(lessThanKey, col);
                    colorPaths.Add(lessThanKey, -1);

                }

                ////outColors.Append(new GH_Colour(col), startPath);
                ////outValues.Append(new GH_Number(double.MinValue), startPath);

                //Mesh[] meshesFromCurves = GetMeshesFromCurves(curvesFromOutline, mp, in col);

                //oLayeredMeshes.AppendRange(meshesFromCurves.Select(m => new GH_Mesh(m)), new GH_Path(g, -1));




                int cuttingCount = 0;
                double previousValue = 0;

                foreach (double currentValue in inputSelector)
                {
                    if (cuttingCount > maxCount)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Too many steps... I reached  {maxCount} and then stopped");
                        break;
                    }

                    //if (gp.BelowMin.HasValue && currentValue < gp.Min)
                    //    continue;


                    if (currentValue > results.Max())
                        break;


                    // Create planes

                    Vector3f moveUpVector = normal * (float)((currentValue - previousValue) * SCALAR);

                    Transform t = Transform.Translation(moveUpVector);

                    GH_Path path = new GH_Path(g, cuttingCount);

                    cuttingPlane.Transform(t);

                    outPlanes.Add(new GH_Plane(cuttingPlane));






                    // Create boundary intersected curves

                    Curve[] intersectedCurves = GetIntersectedCurves(inputMesh, cuttingPlane);



                    if (intersectedCurves != null)
                    {
                        outCurves.AppendRange(intersectedCurves.Select(c => new GH_Curve(c.DuplicateCurve())), path);

                        foreach (var curve in intersectedCurves)
                        {
                            curve.Transform(ProjectToBase);
                        }


                        // Create meshes

                        col = gp.GetColors(new List<double>() { currentValue })[0];



                        meshFromCurves = GetMeshFromCurves(intersectedCurves, mp, in col);


                        



                        if (meshFromCurves != null)
                        {
                            meshFromCurves.Transform(Transform.Translation(0, 0, (cuttingCount + 1) * Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 12.0));

                            //oLayeredMeshes.AppendRange(meshesFromCurves.Select(m => new GH_Mesh(m)), path);
                            oLayeredMeshes.Append(new GH_Mesh(meshFromCurves), path);
                            string key = currentValue >= gp.Max.Value ? $">{currentValue:0.0}" : $"{currentValue:0.0}";
                            if (!colorDescriptions.ContainsKey(key))
                            {
                                colorDescriptions.Add(key, col);
                                colorPaths.Add(key, cuttingCount);

                            }



                        }

                        //if (currentValue >= gp.Max.Value)
                        //    break;
                    }











                    previousValue = currentValue;

                    cuttingCount++;
                }



            }

            foreach (KeyValuePair<string, Color> valuePair in colorDescriptions)
            {
                GH_Path path = new GH_Path(colorPaths[valuePair.Key]);


                outColors.Append(new GH_Colour(valuePair.Value), path);
                outValues.Append(new GH_String(valuePair.Key), path);
            }



            DA.SetDataTree(1, oLayeredMeshes);
            DA.SetDataTree(2, outCurves);
            DA.SetDataList("Planes", outPlanes);
            DA.SetDataList("TempMeshes", previewMeshes);
            DA.SetDataTree(6, outValues);
            DA.SetDataTree(5, outColors);

            #endregion layeredMesh


        }

        private Mesh GetMeshFromCurves(Curve[] intersectedCurves, MeshingParameters mp, in Color col)
        {

            if (intersectedCurves == null || intersectedCurves.Length == 0)
                return null;


            Brep[] brepsFromCurves = Brep.CreatePlanarBreps(intersectedCurves, UnitHelper.FromMeter(0.001));

            if (brepsFromCurves == null)
                return null;

            Mesh[] meshFromBreps = new Mesh[brepsFromCurves.Length];

            for (int j = 0; j < brepsFromCurves.Length; j++)
            {
                Mesh outMesh = new Mesh();
                Mesh[] meshFromBrep = Mesh.CreateFromBrep(brepsFromCurves[j], mp);

                for (int k = 0; k < meshFromBrep.Length; k++)
                {
                    meshFromBrep[k].VertexColors.CreateMonotoneMesh(col);

                }

                for (int i = 0; i < meshFromBrep.Length; i++)
                {
                    outMesh.Append(meshFromBrep[i]);
                }

                meshFromBreps[j] = outMesh;
            }

            return meshFromBreps[0];

        }

        private static Curve[] GetIntersectedCurves(Mesh ínputMesh, in Plane cuttingPlane)
        {
            Polyline[] intersectedPolylines = Rhino.Geometry.Intersect.Intersection.MeshPlane(ínputMesh, cuttingPlane);

            if (intersectedPolylines != null && intersectedPolylines.Length > 0)
            {
                Curve[] curves2 = new Curve[intersectedPolylines.Length];

                for (int j = 0; j < intersectedPolylines.Length; j++)
                {
                    curves2[j] = new PolylineCurve(intersectedPolylines[j]);
                }

                return curves2;
            }

            return null;

        }


        /// <summary>
        /// Returns the mesh extruded
        /// </summary>
        /// <param name="SCALAR"></param>
        /// <param name="OFFSET"></param>
        /// <param name="baseMesh"></param>
        /// <param name="results"></param>
        /// <param name="cuttingPlane"></param>
        /// <returns></returns>
        private static Mesh CreateMeshToBeCut(in double SCALAR, Mesh baseMesh, List<double> results, Plane cuttingPlane)
        {

            var planeBottomToProjectTo = new Plane(cuttingPlane);
            var normal = (Vector3f)(cuttingPlane.ZAxis);




            planeBottomToProjectTo.Transform(Transform.Translation(-cuttingPlane.ZAxis + cuttingPlane.ZAxis * results.Min()));

            ////Moving the bottom "one" down
            ////baseMesh.Translate(-normal * OFFSET);



            //Moving the vertices up
            for (int i = 0; i < results.Count; i++)
            {
                baseMesh.Vertices[i] += normal * (float)(SCALAR * results[i]);
            }

            //Mesh topMesh = meshToCut.DuplicateMesh();

            Mesh edgeMesh = new Mesh();

            Polyline[] edges = baseMesh.GetNakedEdges();


            Transform projectTransformation = Transform.PlanarProjection(planeBottomToProjectTo);


            // Make the edges
            for (int i = 0; i < edges.Length; i++)
            {
                for (int j = 0; j < edges[i].SegmentCount; j++)
                {
                    Mesh msh = new Mesh();
                    Point3d[] pts = new Point3d[4];

                    int id = (j == edges[i].SegmentCount - 1) ? 0 : j + 1;

                    pts[0] = new Point3d(edges[i].X[j], edges[i].Y[j], edges[i].Z[j]);
                    pts[1] = new Point3d(edges[i].X[id], edges[i].Y[id], edges[i].Z[id]);
                    pts[2] = new Point3d(pts[1]);
                    pts[3] = new Point3d(pts[0]);
                    pts[2].Transform(projectTransformation);
                    pts[3].Transform(projectTransformation);

                    msh.Vertices.AddVertices(pts);
                    var fc = new MeshFace(3, 2, 1, 0);

                    msh.Faces.AddFace(fc);

                    edgeMesh.Append(msh);
                }
            }

            baseMesh.Append(edgeMesh);
            baseMesh.Weld(Math.PI); // weld simplifies the mesh, although the render looks more jagged.

            return baseMesh;
            //return normal;
        }

        //public override bool Read(GH_IO.Serialization.GH_IReader reader)
        //{
        //    //targetPanelComponentGuid = reader.GetGuid("targetPanelComponentGuid");

        //    caps = reader.GetBoolean("caps");

        //    return base.Read(reader);
        //}

        //public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        //{
        //    //writer.SetGuid("targetPanelComponentGuid", targetPanelComponentGuid);

        //    writer.SetBoolean("caps", caps);


        //    return base.Write(writer);
        //}


        //public void UpdateMessage()
        //{
        //    Message = $"Cap = {( caps ? "on" : "off")} \nSteps = {steps} \nStepSize = {stepSize:0.0}"; 
        //}


        //private void OnMenu(object sender, EventArgs e)
        //{
        //    //Rhino.RhinoApp.WriteLine($"e is {e}, id is {id}");
        //    //menu.Items[id].Visible = !menu.Items[id].Visible;
        //    //menu.Items[id].Name = "on";
        //    // menuItemCaps.Name = caps ? "Disable cap" : "Enable cap";
        //    caps = !caps;
        //    //Rhino.RhinoApp.WriteLine($"caps is now {(caps ? "on" : "off")}");
        //    this.ExpireSolution(true);
        //    //UpdateMessage();






        //}

        //public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        //{
        //    base.AppendAdditionalMenuItems(menu);
        //    Menu_AppendItem(menu, caps ? "Disable cap" : "Enable cap", OnMenu);


        //    //Menu_AppendGenericMenuItem(menu, "First item");
        //    //Menu_AppendGenericMenuItem(menu, "Second item");
        //    //Menu_AppendGenericMenuItem(menu, "Third item");
        //    //Menu_AppendSeparator(menu);
        //    //Menu_AppendGenericMenuItem(menu, "Fourth item");
        //    //Menu_AppendGenericMenuItem(menu, "Fifth item");
        //    //Menu_AppendGenericMenuItem(menu, "Sixth item");
        //}




        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override System.Guid ComponentGuid
        {
            get { return new Guid("{ed148ded-bbd1-4f18-9768-22376b024930}"); }
        }
    }
}
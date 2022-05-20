using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_Radviewer : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the CS_RadViewer class.
        /// </summary>
        public GH_Radviewer()
          : base("RadViewer", "RadViewer",
              "Radiance viewer. Very inspired by SpiderRad Viewer by Theo Armour\n" +
                "https://github.com/ladybug-tools/spider-rad-viewer",
              "2 Radiance")
        {

        }



        bool TwoSided = false;
        readonly Dictionary<string, RadianceObject> objects = new Dictionary<string, RadianceObject>();
        BoundingBox bb = new BoundingBox();
        readonly Random rnd = new Random();
        readonly List<Curve> failedCurves = new List<Curve>();
        readonly Dictionary<string, System.Drawing.Color> colors = new Dictionary<string, System.Drawing.Color>();


        private HUD hud = new HUD();


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("RadFiles", "RadFiles", "Rad files", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            int p = pManager.AddMeshParameter("Meshes", "Meshes", "Meshes", GH_ParamAccess.list);
            pManager.HideParameter(p);
            pManager.AddTextParameter("Names", "Names", "Names", GH_ParamAccess.list);
            pManager.AddTextParameter("ModifierNames", "ModifierNames", "Modifier names", GH_ParamAccess.list);
            pManager.AddTextParameter("Modifiers", "Modifiers", "Modifiers", GH_ParamAccess.list);
            pManager.AddCurveParameter("FailedWireFrame", "FailedWireFrame", "fail", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            /*
             * The RAD viewer architecture is a setup im testing. I have not benchmarked it but it runs in several steps asynchronously.
             * 1) Read the file in one thread
             * 2) Parse the file into objects in another thread that is waiting on the first thread using BlockingCollections and Task.Wait
             * 3) Cross referencing the Modifiers
             * 
             * The RAD files we're interpreting usually look something like this:
             * Modifier Type Name
             * 0
             * 0
             * N [double] x N
             * 
             * ie:
             * """
             * #Heres a glass polygon
             * glass polygon Window.1
             * 0
             * 0
             * 12
             * 0.0 0.0 0.0
             * 1.0 0.0 0.0
             * 1.0 0.0 1.0
             * 0.0 0.0 1.0
             * """
             */

            BlockingCollection<string> linesPerObject = new BlockingCollection<string>();

            BlockingCollection<RadianceObject> radianceObjects = new BlockingCollection<RadianceObject>();

            List<string> radFiles = DA.FetchList<string>("RadFiles");

            var readLines = Task.Factory.StartNew(() =>
            {
                //Parallel.ForEach(radFiles, radFile => { // maybe not worth disk reading in threads! IO bottleneck.

                if (radFiles == null & radFiles.Count == 0) return;

                foreach (var radFile in radFiles)
                {
                    if (String.IsNullOrEmpty(radFile) || !File.Exists(radFile))
                        //return;
                        continue;

                    StringBuilder currentObject = new StringBuilder(); // current object

                    int c1 = 0;

                    foreach (var line in File.ReadLines(radFile))
                    {

                        if (line == String.Empty || line.StartsWith("#"))
                            continue;

                        if (c1++ % 10 == 0 && GH_Document.IsEscapeKeyDown())
                        {
                            linesPerObject.CompleteAdding();
                            GH_Document GHDocument = OnPingDocument();
                            GHDocument.RequestAbortSolution();
                        }

                        //if (Regex.IsMatch(line, @"^[a-zA-Z]+$")) // there is a word. New Object
                        bool hasLetters = false;
                        for (int i = 0; i < line.Length; i++)
                        {

                            if (line[i] >= 'a' && line[i] <= 'z' || line[i] >= 'A' && line[i] <= 'Z')
                            {
                                hasLetters = true;
                                break;
                            }
                        }
                        if (hasLetters)
                        {
                            if (currentObject.Length > 0)
                            {
                                linesPerObject.Add(currentObject.ToString().Trim());
                            }

                            if (line.Contains("!xform") || line.Contains("-rx") || line.Contains("-f")) // external file?
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "We havent yet added the posibility to parse referenced files. So you may be missing some content!\n" +
                                    $"The line that is left out is {line}");
                                // what to do now?
                            }
                            else
                            {

                                currentObject = new StringBuilder(line);
                                currentObject.Append(" ");

                            }

                        }
                        else // no words
                        {
                            currentObject.Append(line);
                            currentObject.Append(" ");
                        }

                    }
                    linesPerObject.Add(currentObject.ToString().Trim());


                }
                linesPerObject.CompleteAdding();
            });

            //}

            var processLines = Task.Factory.StartNew(() =>
            {
                Debug.WriteLine("starting processing lines");
                int c2 = 0;

                foreach (var line in linesPerObject.GetConsumingEnumerable())
                {
                    try
                    {

                        radianceObjects.Add(ConvertToObject(line));

                    }
                    catch (PolygonException ex)
                    {
                        failedCurves.AddRange(GetFailedLines(line));
                        //radianceObjects.CompleteAdding();
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ex.Message.Substring(0, 100) + "\nCheck the FailedWireFrame output");
                        //throw ex;
                    }

                    if (c2++ % 10 == 0 && GH_Document.IsEscapeKeyDown())
                    {
                        radianceObjects.CompleteAdding();
                        GH_Document GHDocument = OnPingDocument();
                        GHDocument.RequestAbortSolution();
                    }

                }
                radianceObjects.CompleteAdding();
                Debug.WriteLine("done processing lines");
            });



            //

            objects.Clear();

            Debug.WriteLine("starting making objects");

            int counter = 0;

            foreach (var obj in radianceObjects.GetConsumingEnumerable())
            {

                // Thank you james ramsden

                if (counter++ % 10 == 0 && GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }


                if (obj is RaPolygon geo)
                {
                    if (bb.Min == bb.Max)
                        bb = geo.Mesh.GetBoundingBox(false);
                    else
                        bb.Union(geo.Mesh.GetBoundingBox(false));

                    if (objects.ContainsKey(obj.ModifierName))
                    {
                        if (objects[obj.ModifierName] is RaPolygon poly)
                        {

                            poly.AddTempMesh(geo.Mesh);
                        }
                        else
                            throw new Exception($"ehh im not a poly but my modifier name is {obj.ModifierName} and it already exists.");
                    }
                    else
                    {
                        if (!colors.TryGetValue(obj.ModifierName, out System.Drawing.Color color))
                        {
                            color = System.Drawing.Color.FromArgb(rnd.Next(150, 256), rnd.Next(150, 256), rnd.Next(150, 256));
                            colors.Add(obj.ModifierName, color);

                        }

                        geo.Material = new Rhino.Display.DisplayMaterial(color);
                        geo.Material.Emission = geo.Material.Diffuse;
                        geo.Material.IsTwoSided = TwoSided;
                        geo.Material.BackDiffuse = System.Drawing.Color.Red;
                        geo.Material.BackEmission = System.Drawing.Color.Red;


                        objects.Add(obj.ModifierName, geo);
                    }
                }
                else
                {
                    objects.Add("Material_" + obj.Name, obj);
                }

            }

            Debug.WriteLine("done making objects");


            foreach (var pol in objects.Select(obj => obj.Value).OfType<RaPolygon>())
            {
                pol.UpdateMesh();
            }

            Debug.WriteLine("done updating meshes");



            HashSet<string> uniqueMissingModifiers = new HashSet<string>();

            foreach (var obj in objects)
            {
                if (objects.ContainsKey(obj.Value.ModifierName) && obj.Value.ModifierName != objects[obj.Value.ModifierName].ModifierName)
                {
                    obj.Value.Modifier = objects[obj.Value.ModifierName];
                }
                else if (objects.ContainsKey("Material_" + obj.Value.ModifierName))
                {
                    obj.Value.Modifier = objects["Material_" + obj.Value.ModifierName];
                }
                else
                {
                    if (uniqueMissingModifiers.Add(obj.Value.ModifierName) && obj.Value.ModifierName != "void")
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Modifier not found: {obj.Value.ModifierName}. Refered to by {obj.Key}");
                }

            }

            Debug.WriteLine("done setting modifiers");
            hud.Callback.Component = this;
            hud.Callback.Enabled = true;

            hud.Items.Clear();
            
            foreach (RaPolygon poly in objects.Where(o => o.Value is RaPolygon).Select(o => (RaPolygon)o.Value))
            {
                string desc = poly.Modifier is RadianceMaterial m ? m.MaterialDefinition : string.Empty;
                hud.Items.Add(new HUD.HUD_Item() { Name = poly.Name, Description = desc, Mesh = poly.Mesh, Color = System.Drawing.Color.FromArgb(150, 200, 200,200) });
            }
            


            DA.SetDataList(0, objects.Where(o => o.Value is RaPolygon).Select(o => ((RaPolygon)o.Value).Mesh));
            DA.SetDataList(1, objects.Where(o => o.Value is RaPolygon).Select(o => ((RaPolygon)o.Value).Name));
            DA.SetDataList(2, objects.Where(o => o.Value is RaPolygon).Select(o => ((RaPolygon)o.Value).ModifierName));
            DA.SetDataList(3, objects.Where(o => o.Value is RaPolygon).Select(o => (o.Value.Modifier)).Select(m => m is RadianceMaterial ? (m as RadianceMaterial).MaterialDefinition : null));
            DA.SetDataList(4, failedCurves);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            base.RemovedFromDocument(document);


            hud.Callback.Enabled = false;

            hud = null;

            DisplayPipeline.DrawForeground -= DrawForeground;


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

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            Debug.WriteLine("DocumentContextChanged");

            if (hud != null)
            {

                hud.Enabled = false;


                DisplayPipeline.DrawForeground -= DrawForeground;
                if (context == GH_DocumentContext.Loaded)
                {
                    DisplayPipeline.DrawForeground += DrawForeground;

                    hud.Enabled = true;

                }
            }

        }

        protected override void BeforeSolveInstance() //Register new event
        {
            DisplayPipeline.DrawForeground += DrawForeground;
        }


        public Curve[] GetFailedLines(string line)
        {
            const string rep_new_line_re = @"/\s\s+/g";

            string[] data = Regex.Replace(line, rep_new_line_re, " ").Trim().Split(' ').Where(d => !String.IsNullOrEmpty(d)).ToArray();

            if (data.Length < 3)
                return null;

            string type = data[1];

            if (type.Length == 0)
                return null;

            IEnumerable<string> dataNoHeader = data.Skip(6); // skip header

            int i = 0;
            double d1 = -1;
            double d2 = -1;

            List<Point3d> ptList2 = new List<Point3d>();

            foreach (var item in dataNoHeader)
            {
                switch (i++ % 3)
                {
                    case 0:
                        d1 = double.Parse(item, CultureInfo.InvariantCulture);
                        break;
                    case 1:
                        d2 = double.Parse(item, CultureInfo.InvariantCulture);
                        break;
                    case 2:
                        ptList2.Add(new Point3d(d1, d2, double.Parse(item, CultureInfo.InvariantCulture)));
                        break;

                }


            }
            if (ptList2[0] != ptList2[ptList2.Count - 1])
            {
                ptList2.Add(ptList2[0]);
            }


            var segs = new Polyline(ptList2).ToNurbsCurve().DuplicateSegments();

            return Curve.JoinCurves(segs.AsParallel().AsOrdered().Where(s => !IsCurveDup(s, segs)));


        }

        public RadianceObject ConvertToObject(string line)
        {
            const string rep_new_line_re = @"/\s\s+/g";

            string[] data = Regex.Replace(line, rep_new_line_re, " ").Trim().Split(' ').Where(d => !String.IsNullOrEmpty(d)).ToArray();

            if (data.Length < 3)
                return null;

            string type = data[1];

            if (type.Length == 0)
                return null;

            switch (type)
            {
                case "polygon":
                    return new RaPolygon(data);
                case "sphere":
                    return new RaSphere(data);
                case "cone":
                case "cylinder":
                case "plastic":
                case "glass":
                case "metal":
                case "trans":
                case "glow":
                case "mirror":
                case "bsdf":
                default:
                    return new RadianceMaterial(data);
            }


        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1FA443D0-8881-4546-9BA1-259B22CF89B4"); }
        }

        public abstract class RadianceObject
        {
            public string ModifierName;
            public string ObjectType;
            public string Name;
            public RadianceObject Modifier;


            public RadianceObject(string[] data)
            {
                ModifierName = data[0];
                ObjectType = data[1];
                Name = data[2];
            }

        }

        public class RadianceMaterial : RadianceObject
        {
            public string MaterialDefinition { get; set; }

            public RadianceMaterial(string[] data) : base(data)
            {
                //IEnumerable<string> dataNoHeader = data.Skip(6);
                MaterialDefinition = String.Join(" ", data.Take(3)) + "\n" + String.Join("\n", data.Skip(3).Take(3)) + "\n" + String.Join(" ", data.Skip(6));
            }
        }

        public abstract class RadianceGeometry : RadianceObject
        {
            public Rhino.Display.DisplayMaterial Material { get; set; } = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Gray);

            public RadianceGeometry(string[] data) : base(data)
            {

            }

            public abstract void DrawObject(IGH_PreviewArgs args);
            public abstract void DrawWires(IGH_PreviewArgs args);
        }




        public class RaPolygon : RadianceGeometry
        {

            public Mesh Mesh { get; set; }


            readonly List<Mesh> meshes = new List<Mesh>(64);

            public void AddTempMesh(Mesh mesh, bool update = false)
            {
                meshes.Add(mesh);
                if (update)
                    UpdateMesh();
            }

            public void UpdateMesh()
            {
                if (meshes.Count == 0)
                    return;
                if (Mesh == null)
                    Mesh = new Mesh();
                Mesh.Append(meshes);
                meshes.Clear();
            }

            public RaPolygon(string[] data) : base(data)
            {
                IEnumerable<string> dataNoHeader = data.Skip(6); // skip header

                int i = 0;
                double d1 = -1;
                double d2 = -1;

                if (dataNoHeader.Count() <= 12)
                {
                    Mesh = new Mesh();

                    foreach (var item in dataNoHeader)
                    {
                        switch (i++ % 3)
                        {
                            case 0:
                                d1 = double.Parse(item, CultureInfo.InvariantCulture);
                                break;
                            case 1:
                                d2 = double.Parse(item, CultureInfo.InvariantCulture);
                                break;
                            case 2:
                                Mesh.Vertices.Add(new Point3d(d1, d2, double.Parse(item, CultureInfo.InvariantCulture)));
                                break;

                        }



                    }

                    if (Mesh.Vertices.Count == 4)
                    {
                        Mesh.Faces.AddFace(0, 1, 2, 3);
                    }
                    else
                    {
                        Mesh.Faces.AddFace(0, 1, 2);
                    }
                }
                else // Large polygon. more heavy also as it involves a step through BREPs.
                {
                    List<Point3d> ptList2 = new List<Point3d>();

                    foreach (var item in dataNoHeader)
                    {
                        switch (i++ % 3)
                        {
                            case 0:
                                d1 = double.Parse(item, CultureInfo.InvariantCulture);
                                break;
                            case 1:
                                d2 = double.Parse(item, CultureInfo.InvariantCulture);
                                break;
                            case 2:
                                ptList2.Add(new Point3d(d1, d2, double.Parse(item, CultureInfo.InvariantCulture)));
                                break;

                        }


                    }
                    if (ptList2[0] != ptList2[ptList2.Count - 1])
                    {
                        ptList2.Add(ptList2[0]);
                    }
                    // LBT STYLE
                    //var nurbsCurve = new Polyline(ptList2).ToNurbsCurve();
                    //var segs = nurbsCurve.DuplicateSegments();
                    //var borderLines = segs.Where(s => !IsCurveDup(s, segs));
                    //var border = Curve.JoinCurves(borderLines);
                    //var breps = Brep.CreatePlanarBreps(border, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    //var m = Mesh.CreateFromBrep(breps[0], MeshingParameters.Default);
                    //Mesh = m[0];


                    var segs = new Polyline(ptList2).ToNurbsCurve().DuplicateSegments();

                    var border = Curve.JoinCurves(segs.AsParallel().AsOrdered().Where(s => !IsCurveDup(s, segs)));

                    var breps = Brep.CreatePlanarBreps(border, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    var brep = breps != null ? breps[0] : null;

                    if (brep != null)
                        Mesh = Mesh.CreateFromBrep(brep, MeshingParameters.Default)[0];
                    else
                    {

                        throw new PolygonException($"Could not create polygon from:\n{String.Join(" ", data)}");
                    }

                }


            }

            public override void DrawObject(IGH_PreviewArgs args)
            {
                args.Display.DrawMeshShaded(Mesh, Material);
            }

            public override void DrawWires(IGH_PreviewArgs args)
            {
                args.Display.DrawMeshWires(Mesh, System.Drawing.Color.Black, 1);
            }


        }

        public static bool IsCurveDup(Curve crv, Curve[] curves)
        {
            List<Point3d> pts = new List<Point3d>(4) { crv.PointAtStart, crv.PointAtEnd };
            int count = 0;
            foreach (var c in curves)
            {
                if (pts.Contains(c.PointAtStart) && pts.Contains(c.PointAtEnd))
                    count++;
            }

            return count > 1;
        }

        public class RaSphere : RadianceGeometry
        {
            public RaSphere(string[] data) : base(data)
            {
            }

            public override void DrawObject(IGH_PreviewArgs args)
            {
                throw new NotImplementedException();
            }

            public override void DrawWires(IGH_PreviewArgs args)
            {
                throw new NotImplementedException();
            }
        }

        public class ModifierNotFoundException : Exception
        {
            string Msg;
            public override string Message => Msg;

            public ModifierNotFoundException(string msg)
            {
                Msg = msg;
            }
        }

        public override BoundingBox ClippingBox => bb;

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
       
            //ensures that we turn the dashboard on
            if (hud != null && !this.Hidden && !this.Locked && hud.Items.Count > 0)
            {
                hud.Enabled = true;
                hud.Callback.Enabled = true;

                if (hud.HighlightedItem != null )
                {
                    args.Display.DrawMeshShaded(hud.HighlightedItem.Mesh, new DisplayMaterial(System.Drawing.Color.Red));
                }

            }

       
        

            foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            {
                obj.DrawObject(args);
            }

            //foreach( Curve crv in failedCurves)
            //{
            //    args.Display.DrawCurve(crv, System.Drawing.Color.Red);
            //}

            base.DrawViewportMeshes(args);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            {
                obj.DrawWires(args);
            }

            //foreach( Curve crv in failedCurves)
            //{
            //    args.Display.DrawCurve(crv, System.Drawing.Color.Red);
            //}

            base.DrawViewportWires(args);
        }

        public void ToggleTwoSided()
        {
            TwoSided = !TwoSided;

            foreach (RadianceGeometry obj in objects.Where(o => o.Value is RadianceGeometry).Select(o => o.Value))
            {
                obj.Material.IsTwoSided = TwoSided;
            }
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("IsTwoSided", ref TwoSided);
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("IsTwoSided", TwoSided);
            return base.Write(writer);
        }



        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Toggle Twosided", (s, e) =>
            {
                ToggleTwoSided();
            }, true, TwoSided);
        }


        public override bool IsPreviewCapable => true;



    }

    [Serializable]
    internal class PolygonException : Exception
    {
        public PolygonException()
        {
        }

        public PolygonException(string message) : base(message)
        {
        }

        public PolygonException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PolygonException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
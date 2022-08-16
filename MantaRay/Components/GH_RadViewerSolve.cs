using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

using System.Globalization;
using System.IO;
using System.Linq;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Grasshopper.Kernel;

using Rhino.Geometry;

using MantaRay.RadViewer;
using MantaRay.RadViewer.HeadsUpDisplay;

namespace MantaRay.Components
{
    public partial class GH_RadViewerSolve : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the CS_RadViewer class.
        /// </summary>
        public GH_RadViewerSolve()
          : base("RadViewer", "RadViewer",
              "Radiance viewer. Very inspired by SpiderRad Viewer by Theo Armour\n" +
                "https://github.com/ladybug-tools/spider-rad-viewer",
              "2 Radiance")
        {

        }

        public bool isRunning = true;
        public bool wasHidden = false;

        public bool TwoSided = false;
        public bool ShowEdges = true;
        public bool Transparent = true;

        public bool Polychromatic = true;

        /// <summary>
        /// A list of all objects sort by modifier name
        /// </summary>
        readonly Dictionary<string, RadianceObjectCollection> objects = new Dictionary<string, RadianceObjectCollection>();
        readonly Dictionary<string, RadianceMaterial> modifiers = new Dictionary<string, RadianceMaterial>();

        BoundingBox? bb = null;
        readonly Random rnd = new Random();
        readonly List<Curve> failedCurves = new List<Curve>();
        readonly Dictionary<string, System.Drawing.Color> colors = new Dictionary<string, System.Drawing.Color>();

        public List<string> ErrorMsgs = new List<string>();


        private HUD hud = new HUD();

        TimeSpan timeSpan = default;



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("RadFiles", "RadFiles", "Rad files", GH_ParamAccess.list)].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            int p = pManager.AddGeometryParameter("Geo", "Geo", "Geo", GH_ParamAccess.list);
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

            if (!isRunning)
            {
                DA.SetDataList(0, objects.OrderBy(o => o.Key).Select(o => o.Value.GetGeometry(false)));
                DA.SetDataList(1, objects.OrderBy(o => o.Key).Select(o => o.Value.GetName()));
                DA.SetDataList(2, objects.OrderBy(o => o.Key).Select(o => o.Value.ModifierName));
                //DA.SetDataList(3, objects.OrderBy(o => o.Key).Select(o => (o.Value.Modifier)).Select(m => m is RadianceMaterial ? (m as RadianceMaterial).MaterialDefinition : null));
                DA.SetDataList(4, failedCurves);
                isRunning = true;

                this.Hidden = wasHidden;

                foreach (string msg in ErrorMsgs)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, msg);
                }
                return;
            }


            ErrorMsgs.Clear();
            objects.Clear();
            failedCurves.Clear();



            //this.Locked = true;
            timeSpan = new TimeSpan(0);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            wasHidden = this.Hidden;
            this.Hidden = true;

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



            List<string> radFiles = DA.FetchList<string>("RadFiles");

            //var readLines = Task.Factory.StartNew(() =>
            //{


            if (radFiles == null & radFiles.Count == 0) return;

            foreach (var radFile in radFiles)
            {
                if (String.IsNullOrEmpty(radFile) || !File.Exists(radFile))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{radFile} not found.");
                    continue;
                }

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


                    if (line.HasLetters())
                    {
                        if (currentObject.Length > 0)
                        {
                            linesPerObject.Add(currentObject.ToString().Trim());
                        }

                        if (line.Contains("!xform") || line.Contains("-rx") || line.Contains("-f")) // external file?
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "We havent yet added the posibility to parse referenced files. So you may be missing some content!\n" +
                                $"The line that is left out is {line}");
                            // TODO: Add linked objects
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
            //});



            BlockingCollection<RadianceObject> radianceObjects = new BlockingCollection<RadianceObject>();



            //var processLines = Task.Factory.StartNew(() =>
            //{
            Debug.WriteLine("starting processing lines");


            int c2 = 0;

            foreach (var line in linesPerObject.GetConsumingEnumerable())
            {
                try
                {

                    radianceObjects.Add(RadianceObject.FromString(line));

                }
                catch (RaPolygon.PolygonException ex)
                {
                    failedCurves.AddRange(GetFailedLines(line));
                    //radianceObjects.CompleteAdding();
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, ex.Message.Substring(0, 100) + "\nCheck the FailedWireFrame output");
                    ErrorMsgs.Add(ex.Message);
                    //throw ex;
                }
                catch (RaPolygon.SyntaxException e)
                {
                    ErrorMsgs.Add("SyntaxError: " + e.Message);

                }
                catch (Exception e)
                {
                    ErrorMsgs.Add("Other error: " + e.Message);

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






            Debug.WriteLine("starting making objects");



            foreach (var obj in radianceObjects.GetConsumingEnumerable())
            {

                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }

                try
                {
                    switch (obj)
                    {
                        case IHasPreview previewableObject:
                            if (!bb.HasValue || bb.HasValue && bb.Value.Min == bb.Value.Max)
                                bb = previewableObject.GetBoundingBox();
                            else if (previewableObject.GetBoundingBox() != null)
                                bb?.Union(previewableObject.GetBoundingBox().Value);

                            if (objects.ContainsKey(obj.ModifierName) && obj is RadianceGeometry g)
                            {
                                objects[obj.ModifierName].AddObject(g);
                            }

                            break;

                        case RadianceMaterial mat:
                            modifiers.Add(mat.Name, mat);

                            break;

                        default:

                            break;
                    }
                }
                catch (Exception e)
                {
                    ErrorMsgs.Add("Create objs: " + e.Message);
                }


            }

            Debug.WriteLine("done making objects");


            foreach (RadianceObjectCollection obj in objects.Values)
            {
                obj.UpdateMesh();
            }

            Debug.WriteLine("done updating meshes");

            HashSet<string> uniqueMissingModifiers = new HashSet<string>();

            //try
            //{

            foreach (RadianceObjectCollection collection in objects.Values)
            {
                foreach (var obj in collection.Objects)
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

            }

            //}
            //catch (Exception e)
            //{
            //    ErrorMsgs.Add("modifiers: " + e.Message);
            //}

            Debug.WriteLine("done setting modifiers");

            SetupHUD();

            //this.Locked = false;
            isRunning = false;
            timeSpan = new TimeSpan(0, 0, 0, 0, (int)sw.ElapsedMilliseconds);
            sw.Stop();
            this.ExpireSolution(true);

            //});


        }







        /// <summary>
        /// _Very_ inspired by the Ladybug Tools component
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
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

            return Curve.JoinCurves(segs.AsParallel().AsOrdered().Where(s => !RaPolygon.IsCurveDup(s, segs)));

        }


    }


}
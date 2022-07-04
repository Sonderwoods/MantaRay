using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using GrasshopperRadianceLinuxConnector.Components;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_AnnualResults : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_AnnualResults class.
        /// </summary>
        public GH_AnnualResults()
          : base("AnnualResults", "AnnualResults",
              "Read Ill files...",
              "2 Radiance")
        {
        }

        double[] OldNumberResults = new double[0];

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("illFile", "illFile", "illFile", GH_ParamAccess.item);
            pManager.AddIntegerParameter("schedule", "schedule", "schedule as 0s and 1s. Should be 8760 long??", GH_ParamAccess.list);
            pManager[pManager.AddNumberParameter("min", "min", "min lux level, default is 300", GH_ParamAccess.item, 300)].Optional = true;
            pManager[pManager.AddNumberParameter("max", "max", "max lux level, default is 0", GH_ParamAccess.item, 0)].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)

        {
            pManager.AddTextParameter("Headers", "Headers", "Headers\ntypically rows = hours and columns = points", GH_ParamAccess.list);
            pManager.AddNumberParameter("Results", "Results", "Results", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            CheckIfRunOrUseOldResults(DA, 0); //template
            if (!CheckIfRunOrUseOldResults(DA, 1, OldNumberResults)) return; //template


            string illFile = DA.Fetch<string>("illFile");
            List<string> headerLines = new List<string>(8);
            var linesPerHour = new BlockingCollection<string>();
            bool[] schedule = DA.FetchList<int>("schedule").AsParallel().AsOrdered().Select(s => s >= 1).ToArray();
            int headerRows = 0;
            int headerColumns = 0;
            int readLinesCounter = 0;

            var readLines = Task.Factory.StartNew(() =>
            {
                bool begin = false;
                

                if (String.IsNullOrEmpty(illFile) || !File.Exists(illFile))
                    return;

                foreach (var line in File.ReadLines(illFile))
                {
                    if (!begin)
                    {
                        if (line.Length == 0)
                            begin = true;
                        else
                        {
                            headerLines.Add(line);
                            if (line.StartsWith("NCOLS"))
                                headerColumns = int.Parse(line.Split('=')[1]);
                            if (line.StartsWith("NROWS"))
                                headerRows = int.Parse(line.Split('=')[1]);
                        }
                    }
                    else
                    {
                        //if (counter == 0)
                        // pointCount = line.Split('\t').Length + 1;
                        if (schedule[readLinesCounter]) //<<-- TO FILTER ROWS BY SCHEDULE
                            linesPerHour.Add(line);
                        Interlocked.Increment(ref readLinesCounter);

                    }

                }
                

                linesPerHour.CompleteAdding();
            });

            if (!DA.Fetch<bool>("Run"))
            {

                DA.SetData("Ran", false);
                Task.WaitAll(readLines);
                DA.SetDataList("Headers", headerLines);
                return;
            }


            double max = DA.Fetch<double>("max");
            if (max <= 0)
                max = double.MaxValue;

            double min = DA.Fetch<double>("min");



            int scheduleHoursCount = schedule.Count(s => s);


            double[] wellLitHoursPerPoint = new double[0];


            int pointCount = 0;
            int filteredLineCount = 0;

            var processLines = Task.Factory.StartNew(() =>
            {
                foreach (var line in linesPerHour.GetConsumingEnumerable())
                {
                    Interlocked.Increment(ref filteredLineCount);

                    var resultsPerPointPerHour = line.Split('\t')
                        .AsParallel()
                        .AsOrdered()
                        .Where(x => !String.IsNullOrWhiteSpace(x))
                        .Select(x => double.Parse(x.Trim(' '), CultureInfo.InvariantCulture))//.ToArray();
                        .Select(x => x >= min && x <= max ? 1 : 0)
                        .ToArray();

                    pointCount = resultsPerPointPerHour.Length;

                    if (wellLitHoursPerPoint.Length == 0)
                        wellLitHoursPerPoint = new double[resultsPerPointPerHour.Length];

                    for (int i = 0; i < resultsPerPointPerHour.Length; i++)
                    {
                        wellLitHoursPerPoint[i] += resultsPerPointPerHour[i];
                    }



                }

                if (filteredLineCount != scheduleHoursCount)
                    throw new Exception($"Schedule hours count  ({scheduleHoursCount}) does not match the hours in ill file  ({filteredLineCount})!");

            });


            DA.SetData("Ran", true);

            Task.WaitAll(readLines);

            OldResults = headerLines.ToArray();
            DA.SetDataList("Headers", OldResults);


            Task.WaitAll(processLines);

            if (headerRows != 0 && headerRows != readLinesCounter)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"NROWS={headerRows}, but the file contained {readLinesCounter} lines.");
            }

            if (headerColumns != 0 && headerColumns != pointCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"NROWS={headerColumns}, but the file contained {pointCount} columns.");
            }


            OldNumberResults = wellLitHoursPerPoint.Select(r => r / (double)scheduleHoursCount).ToArray();

            DA.SetDataList("Results", OldNumberResults);


        }

        public override bool Read(GH_IReader reader)
        {
            List<double> oldNumbers = new List<double>();
            double v = 0;
            int i = 0;
            while (reader.TryGetDouble("numbers", i++, ref v))
            {
                oldNumbers.Add(v);
            }
            OldNumberResults = oldNumbers.ToArray();

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.RemoveChunk("numbers");
            for (int i = 0; i < OldNumberResults.Length; i++)
            {
                writer.SetDouble("numbers", i, OldNumberResults[i]);
            }
            

            return base.Write(writer);
        }


        public override Guid ComponentGuid => new Guid("2359E897-EADC-440D-8052-3DEBF31CB972");

    }
}
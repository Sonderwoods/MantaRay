using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    [Obsolete]
    public class GH_AnnualResultsTransposed : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_AnnualResults class.
        /// </summary>
        public GH_AnnualResultsTransposed()
          : base("AnnualResults(old)", "AnnualResults(old)",
              "Read Ill files...",
              "2 Radiance")
        {
        }

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
            pManager.AddTextParameter("Headers", "Headers", "Headers", GH_ParamAccess.list);
            pManager.AddNumberParameter("Results", "Results", "Results", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Number of input hours", "in hours", "in hours", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Number of hours in schedule", "sched hours", "schedule hours", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!DA.Fetch<bool>("Run"))
            {
                DA.SetData("Ran", false);
            }
            else
            {

                double max = DA.Fetch<double>("max");
                if (max <= 0)
                    max = double.MaxValue;

                double min = DA.Fetch<double>("min");

                bool[] schedule = DA.FetchList<int>("schedule").AsParallel().AsOrdered().Select(s => s >= 1).ToArray();

                int totalHours = schedule.Count(s => s);


                string illFile = DA.Fetch<string>("illFile");

                var inputLines = new BlockingCollection<string>();

                List<string> headerLines = new List<string>(8);


                var readLines = Task.Factory.StartNew(() =>
                {
                    bool begin = false;
                    //int counter = 0;
                    foreach (var line in File.ReadLines(illFile))
                    {
                        if (!begin)
                        {
                            if (line.Length == 0)
                                begin = true;
                            else
                                headerLines.Add(line);
                        }
                        else
                        {
                            //if (schedule[counter++]) //<<-- TO FILTER ROWS BY SCHEDULE
                            inputLines.Add(line);

                        }

                    }

                    inputLines.CompleteAdding();
                });

                // NEED TO REWRITE ... OR TRANSPOSE MATRIX FIRST. PERHAPS THE LATER IS EASIER

                ConcurrentDictionary<int, double> results = new ConcurrentDictionary<int, double>();

                int lineNumber = -1;

                var processLines = Task.Factory.StartNew(() =>
                {
                    Parallel.ForEach(inputLines.GetConsumingEnumerable(), line =>
                    {
                        Interlocked.Increment(ref lineNumber);

                        string[] fields = line.Split('\t');

                        results.TryAdd(lineNumber,
                            fields
                            .Where((x, index) => schedule[index]) // <-- TO FILTER COLUMN BY SCHEDULE
                            .Where(x => !String.IsNullOrWhiteSpace(x))
                            .Select(x => double.Parse(x.Trim(' ')))
                            .Count(x => x >= min && x <= max) / (double)fields.Length
                            );

                    });
                });

                DA.SetDataList("Headers", headerLines);
                DA.SetData("Number of hours in schedule", totalHours);
                DA.SetData("Ran", true);

                Task.WaitAll(readLines);
                DA.SetData("Number of input hours", inputLines.Count);

                Task.WaitAll(readLines, processLines);


                DA.SetDataList("Results", results.OrderBy(x => x.Key).Select(x => x.Value));
            }

        }


        public override Guid ComponentGuid => new Guid("23592897-EADC-420D-8052-3DEBF31CB972");

    }
}
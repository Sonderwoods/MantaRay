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
    public class GH_AnnualResults : GH_Template
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
                return;
            }


            double max = DA.Fetch<double>("max");
            if (max <= 0)
                max = double.MaxValue;

            double min = DA.Fetch<double>("min");

            bool[] schedule = DA.FetchList<int>("schedule").AsParallel().AsOrdered().Select(s => s >= 1).ToArray();

            int totalHours = schedule.Count(s => s);


            string illFile = DA.Fetch<string>("illFile");

            var linesPerHour = new BlockingCollection<string>();

            List<string> headerLines = new List<string>(8);

            //int pointCount = 0;


            var readLines = Task.Factory.StartNew(() =>
            {
                bool begin = false;
                int counter = 0;
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
                        //if (counter == 0)
                           // pointCount = line.Split('\t').Length + 1;
                        if (schedule[counter++]) //<<-- TO FILTER ROWS BY SCHEDULE
                            linesPerHour.Add(line);

                    }

                }

                linesPerHour.CompleteAdding();
            });

            double[] wellLitHoursPerPoint = new double[0];

            //ConcurrentDictionary<int, double> results = new ConcurrentDictionary<int, double>();

            //int lineNumber = -1;

            //var processLines = Task.Factory.StartNew(() =>
            //{
            foreach (var line in linesPerHour.GetConsumingEnumerable())
            {
                //Interlocked.Increment(ref lineNumber);

                //string[] fields = line.Split('\t');

                var resultsPerPointPerHour = line.Split('\t')
                .AsParallel()
                .AsOrdered()
                //.Where((x, index) => schedule[index]) // <-- TO FILTER COLUMN BY SCHEDULE
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Select(x => double.Parse(x.Trim(' ')))
                .Select(x => x >= min && x <= max ? 1 : 0)
                .ToArray();

                if (wellLitHoursPerPoint.Length == 0)
                    wellLitHoursPerPoint = new double[resultsPerPointPerHour.Length];

                for (int i = 0; i < resultsPerPointPerHour.Length; i++)
                {
                    wellLitHoursPerPoint[i] += resultsPerPointPerHour[i];
                }





            }




            //});

            DA.SetDataList("Headers", headerLines);
            
            DA.SetData("Ran", true);

            Task.WaitAll(readLines);

            

            //Task.WaitAll(readLines, processLines);




            DA.SetDataList("Results", wellLitHoursPerPoint.Select(r => r / totalHours));


        }


        public override Guid ComponentGuid => new Guid("2359E897-EADC-440D-8052-3DEBF31CB972");

    }
}
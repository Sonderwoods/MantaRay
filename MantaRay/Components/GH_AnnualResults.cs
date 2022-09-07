using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using Rhino.Commands;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_AnnualResults : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_AnnualResults class.
        /// </summary>
        public GH_AnnualResults()
          : base("AnnualResults", "AnnualResults",
              "Read Ill files...",
              "3 Results")
        {
        }


        Dictionary<string, double[]> results = new Dictionary<string, double[]>();
        Dictionary<string, DateTime> resultsLastModified = new Dictionary<string, DateTime>();
        double lastMin = -1;
        double lastMax = -1;
        double[] OldNumberResults = new double[0];

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("illFile", "illFile", "illFile", GH_ParamAccess.list);
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

            if (!DA.Fetch<bool>("Run"))
            {

                DA.SetData("Ran", false);

                return;
            }


            double max = DA.Fetch<double>("max");
            if (max <= 0)
                max = double.MaxValue;

            double min = DA.Fetch<double>("min");

            if (min != lastMin || max != lastMax)
            {
                results.Clear();
                resultsLastModified.Clear();
            }
            lastMin = min;
            lastMax = max;




            //List<int> pointsPerFile = new List<int>();
            List<string> illFiles = DA.FetchList<string>("illFile");
            List<string> headerLines = new List<string>(8);
            var linesPerHour = new BlockingCollection<string>();
            bool[] schedule = DA.FetchList<int>("schedule").AsParallel().AsOrdered().Select(s => s >= 1).ToArray();
            int headerRows = 0;
            List<int> headerColumns = new List<int>();
            //int dataLineCount = 0;

            List<string> illFilesToUpdate = new List<string>();

            foreach (var key in resultsLastModified.Keys.ToArray())
            {
                if (!illFiles.Contains(key))
                {
                    resultsLastModified.Remove(key);
                    results.Remove(key);
                }
            }

            foreach (var file in illFiles)
            {
                if (!resultsLastModified.Keys.Contains(file) || (!string.IsNullOrEmpty(file) && File.Exists(file) && File.GetLastWriteTime(file) > resultsLastModified[file]))
                {
                    illFilesToUpdate.Add(file);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Blank, "Recalculating " + file);
                }

            }

            object lockPointsPerFile = new object();
            //int ppf = 0;

            var readLines = Task.Factory.StartNew(() =>
            {




                for (int i = 0; i < illFilesToUpdate.Count; i++)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    //Interlocked.Exchange(ref ppf, 0);
                    int dataLineCount = 0;
                    bool begin = false;
                    var illFile = illFilesToUpdate[i];

                    if (String.IsNullOrEmpty(illFile))
                    {
                        results[""] = new double[] { 0.0 };
                        resultsLastModified[""] = DateTime.Now;
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Blank, "An empty file is used. Outputting 0");
                        continue;
                    }

                    if (!File.Exists(illFile))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The is file not found. Did you convert it to a windows path?\n" + illFile);
                        //throw new FileNotFoundException("The is file not found. Did you convert it to a windows path?\n" + illFile);
                        //throw new FileNotFoundException("The is file not found. Did you convert it to a windows path?\n" + illFile);
                        continue;
                        //return;
                    }

                    int lineCount = 0;

                    foreach (var line in File.ReadLines(illFile))
                    {

                        lineCount++;

                        if (!begin)
                        {
                            if (line.Trim().Length == 0)
                                begin = true;
                            else
                            {
                                headerLines.Add(line);
                                if (line.StartsWith("NCOLS"))
                                {
                                    headerColumns.Add(int.Parse(line.Split('=')[1]));
                                    if (headerColumns[headerColumns.Count - 1] == 8760)
                                    {
                                        throw new IndexOutOfRangeException("Looks like you need to transpose your matrix first! We have 8760 cols and I need 8760 rows.\n" +
                                            "Use \"rcollate -t input.ill > output.ill\" to perform the task.");
                                    }
                                }
                                if (line.StartsWith("NROWS"))
                                {
                                    headerRows = int.Parse(line.Split('=')[1]);
                                    //Interlocked.Exchange(ref ppf, headerRows);

                                }
                            }
                        }
                        else
                        {

                            if (dataLineCount >= schedule.Length)
                                throw new System.IndexOutOfRangeException($"Somehow the ill file has more rows than your schedule!\nLast line was at {lineCount}: {line}") { Source = illFile };

                            if (line.Trim().Length == 0)
                                break; // avoid empty lines in end of ill file

                            if (schedule[dataLineCount]) //<<-- TO FILTER ROWS BY SCHEDULE
                                linesPerHour.Add(line);
                            dataLineCount++;

                        }



                    }

                    //if (headerRows != 0 && headerRows != dataLineCount)
                    //{
                    //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"NROWS={headerRows}, but the file contained {dataLineCount} lines. (file was {illFilesToUpdate[i]}");
                    //}

                    linesPerHour.Add("END");




                    //pointsPerFile.Add(dataLineCount);

#if DEBUG
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Readlines for {illFiles[i]} took {sw.Elapsed.ToShortString()}");
#endif
                }



                linesPerHour.CompleteAdding();


            });





            int scheduleHoursCount = schedule.Count(s => s);

            int pointCount = 0;
            int filteredLineCount = 0;
            int localCounter = 0;
            int fileCounter = 0;


            var processLines = Task.Factory.StartNew(() =>
            {
                double[] wellLitHoursPerPoint = null;


                foreach (var line in linesPerHour.GetConsumingEnumerable())
                {

                    Interlocked.Increment(ref localCounter);

                    if (string.Equals(line, "END"))
                    {


                        results[illFilesToUpdate[fileCounter]] = wellLitHoursPerPoint.Select(r => r / (double)scheduleHoursCount).ToArray();
                        resultsLastModified[illFilesToUpdate[fileCounter]] = DateTime.Now;

                        wellLitHoursPerPoint = null;

                        if (filteredLineCount != scheduleHoursCount && filteredLineCount > 0)
                            throw new Exception($"Schedule hours count  ({scheduleHoursCount}) does not match the hours in ill file  ({filteredLineCount})\n{illFilesToUpdate[fileCounter]}!");

                        if (headerColumns[fileCounter] != 0 && headerColumns[fileCounter] != pointCount)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"NROWS={headerColumns}, but the file contained {pointCount} columns.\n{illFilesToUpdate[fileCounter]}");
                        }


                        Interlocked.Increment(ref fileCounter);
                        Interlocked.Exchange(ref localCounter, 0);
                        Interlocked.Exchange(ref filteredLineCount, 0);



                        continue;
                    }

                    Interlocked.Increment(ref filteredLineCount);

                    var resultsPerPointForThisHour = line.Split('\t')
                        .AsParallel()
                        .AsOrdered()
                        .Where(x => !String.IsNullOrWhiteSpace(x))
                        .Select(x => double.Parse(x.Trim(' '), CultureInfo.InvariantCulture))//.ToArray();
                        .Select(x => x >= min && x <= max ? 1 : 0)
                        .ToArray();

                    //Interlocked.Exchange(ref filteredLineCount, resultsPerPointForThisHour.Length);
                    pointCount = resultsPerPointForThisHour.Length;

                    if (wellLitHoursPerPoint == null)
                        wellLitHoursPerPoint = new double[resultsPerPointForThisHour.Length];

                    for (int i = 0; i < resultsPerPointForThisHour.Length; i++)
                    {
                        wellLitHoursPerPoint[i] += resultsPerPointForThisHour[i];
                    }





                }


            });


            DA.SetData("Ran", true);

            try
            {
                while (true)
                {
                    if (Task.WaitAll(new Task[] { readLines }, 100))
                    {
                        break;
                    }
                    if (GH_Document.IsEscapeKeyDown())
                    {

                        return;
                    }

                }
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    ae.Handle(ex =>
                    {
                        throw e;
                    });
                }
            }



            DA.SetDataList("Headers", OldResults);

            try
            {
                Task.WaitAll(processLines);
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    ae.Handle(ex =>
                    {
                        throw e;
                    });
                }
            }

            GH_Structure<GH_Number> outResults = new GH_Structure<GH_Number>();
            for (int i = 0; i < illFiles.Count; i++)
            {
                GH_Path p = new GH_Path(i);
                outResults.AppendRange(results[illFiles[i]].Select(r => new GH_Number(r)), p);
            }







            DA.SetDataTree(1, outResults);


        }

        public override void ClearCachedData()
        {
            results.Clear();
            resultsLastModified.Clear();
            base.ClearCachedData();
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


            //writer.RemoveChunk("numbers");
            for (int i = 0; i < OldNumberResults.Length; i++)
            {
                writer.SetDouble("numbers", i, OldNumberResults[i]);
            }


            return base.Write(writer);
        }


        public override Guid ComponentGuid => new Guid("2359E897-EADC-440D-8052-3DEBF31CB972");

    }
}
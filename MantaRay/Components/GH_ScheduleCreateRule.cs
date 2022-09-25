using System;
using System.Collections.Generic;

using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_ScheduleCreateRule : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_ToLinux class.
        /// </summary>
        public GH_ScheduleCreateRule()
          : base("ScheduleRule", "ScheduleRule",
              "Select XX hours of the year",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("Month(s)", "Month(s)", "Months to include in the schedule. Accepts\n" +
                "List of numbers, ie 3,4,5\n" +
                "Interval/Domain, ie \"2 to 10\"\n" +
                "For all, leave empty", GH_ParamAccess.list, "")].Optional = true;
            pManager[pManager.AddTextParameter("Day(s)", "Day(s)", "Days to include in the schedule. Accepts\n" +
                "List of numbers, ie 3,4,5\n" +
                "Interval/Domain, ie \"2 to 10\"\n" +
                "For all, leave empty", GH_ParamAccess.list, "")].Optional = true;
            pManager[pManager.AddTextParameter("Hour(s)", "Hour(s)", "Hours to include in the schedule. Accepts\n" +
                "List of numbers, ie 3,4,5\n" +
                "Interval/Domain, ie \"2 to 10\"\n" +
                "For all, leave empty", GH_ParamAccess.list, "")].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Output Schedule", "Schedule", "Schedule", GH_ParamAccess.list);
            pManager.AddIntegerParameter("HOYS", "HOYS", "Schedule", GH_ParamAccess.list);
            pManager.AddTimeParameter("Dates", "Dates", "Dates", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<string>[] inLists = new List<string>[3] { DA.FetchList<string>(0), DA.FetchList<string>(1), DA.FetchList<string>(2) };
            bool[][] outBools = new bool[3][] { new bool[12], new bool[31], new bool[24] };
            List<GH_Time> dates = new List<GH_Time>();

            bool[] hoys = new bool[8760];

            for (int i = 0; i < inLists.Length; i++)
            {
                foreach (string item in inLists[i])
                {
                    int[] entries = StringHelper.GetNumbers(item);
                    if (entries != null)
                    {
                        foreach (int entry in entries)
                        {
                            outBools[i][entry] = true;
                        }
                    }
                        
                }
            }


            List<GH_Integer> outHours = new List<GH_Integer>();

            DateTime time = new DateTime(2011, 1, 1);

            for (int i = 0; i < hoys.Length; i++)
            {
                
                if ((outBools[0][time.Month - 1] || outBools[0].Where(c => c).Count() == 0) && 
                    (outBools[1][time.Day - 1] || outBools[1].Where(c => c).Count() == 0) &&
                    (outBools[2][time.Hour] || outBools[02].Where(c => c).Count() == 0))
                {
                    hoys[i] = true;
                    outHours.Add(new GH_Integer(i + 1));
                    dates.Add(new GH_Time(new DateTime(2011, time.Month, time.Day, time.Hour, 0, 0)));

                }
                time = time.AddHours(1);

            }
            DA.SetDataList(0, hoys);
            DA.SetDataList(1, outHours);
            DA.SetDataList(2, dates);



        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("AF816E54-D307-4E15-9FDD-3DD9B0D361ED"); }
        }
    }
}
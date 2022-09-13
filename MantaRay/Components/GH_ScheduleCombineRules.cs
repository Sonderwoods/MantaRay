//using System;
//using System.Collections.Generic;

//using System.Drawing;
//using System.Linq;
//using System.Xml.Linq;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Data;
//using Grasshopper.Kernel.Types;
//using MantaRay.Components;
//using Rhino.Geometry;

//namespace MantaRay.Components
//{
//    public class GH_ScheduleCombineRules : GH_Template
//    {
//        /// <summary>
//        /// Initializes a new instance of the GH_ToLinux class.
//        /// </summary>
//        public GH_ScheduleCombineRules()
//          : base("Combine Schedules", "Combine Schedules",
//              "Select XX hours of the year",
//              "2 Radiance")
//        {
//        }

//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddIntegerParameter("First Schedule", "1st Schedule", "Initial schedule that the others will added or substracted from.", GH_ParamAccess.list);
//            pManager.AddIntegerParameter("Schedules", "Schedules", "Will add the schedules together in the branch order they appear. If you want to remove hours in a schedule then put the schedule through a NEG component so 1 becomes -1", GH_ParamAccess.tree);


//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddIntegerParameter("Output Schedule", "Schedule", "Schedule", GH_ParamAccess.list);
//        }

//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            GH_Structure<GH_Integer> inSchedules = DA.FetchTree<GH_Integer>(1);

//            int scheduleLength = 0;

//            int[] currentSchedule = null;

//            for (int i = 0; i < inSchedules.PathCount; i++)
//            {
//                List<GH_Integer> items = (List<GH_Integer>)inSchedules.get_Branch(i);

//                if (currentSchedule == null)
//                {
//                    currentSchedule = DA.FetchList<int>(0).ToArray();
//                    scheduleLength = items.Count;
//                    continue;
//                }


//                if (scheduleLength != items.Count)
//                    throw new Exception($"List lengths don't match!.. Path {i - 1} has {scheduleLength} items, and path {i} has {items.Count} items");

//                for (int j = 0; j < currentSchedule.Length; j++)
//                {
//                    currentSchedule[j] = Math.Min(Math.Max(currentSchedule[j] + items[j].Value, 1), 0);
//                }

//            }

//            DA.SetDataList(0, currentSchedule);

//        }



//        /// <summary>
//        /// Gets the unique ID for this component. Do not change this ID after release.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("AF816E54-D327-4E15-9FDD-3DA230D3B1ED"); }
//        }
//    }
//}
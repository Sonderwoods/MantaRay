using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_ApplyOverrides : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_ApplyGlobals class.
        /// </summary>
        public GH_ApplyOverrides()
          : base("Apply Overrides", "Overrides",
              "Adds the overrides to the text element. This is mainly as a test component as it should be automatically applied to all text inputs in the other components.",
              "0 Setup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Input", "Input", "Input", GH_ParamAccess.list);
            pManager[pManager.AddTextParameter("Additional Keys", "Keys", "Keys", GH_ParamAccess.list, new List<string>())].Optional = true;
            pManager[pManager.AddTextParameter("Additional Values", "Values", "Values", GH_ParamAccess.list, new List<string>())].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "O", "output with globals applied", GH_ParamAccess.list);
            pManager.AddTextParameter("Pairs", "K,V", "Pairs", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<string> keys = DA.FetchList<string>("Additional Keys");
            List<string> values = DA.FetchList<string>("Additional Values");
            List<string> outPairs = new List<string>(keys.Count);
            List<string> inputs = DA.FetchList<string>("Input");


            if (keys.Count != values.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "List lengths are not matching");
            }

            int keysLength = 5;
            if (GlobalsHelper.Globals.Keys.Count > 0)
            {
                keysLength = GlobalsHelper.Globals.Keys.Select(k => k.Length).Max();
            }
            if (keys.Count > 0)
            {
                keysLength = Math.Max(keysLength, keys.Select(k => k.Length).Max());
            }

            foreach (KeyValuePair<string, string> item in GlobalsHelper.Globals)
            {
                outPairs.Add($"<{item.Key}> --> {item.Value}");
                outPairs.Add($"{("<" + item.Key + ">").PadRight(keysLength + 1)} --> {item.Value}");
            }

            if (keys.Count == 0 && values.Count == 0)
            {
                DA.SetDataList(0, inputs.Select(s => s.AddGlobals()));
                DA.SetDataList(1, outPairs);
                return;
            }


            Dictionary<string, string> locals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            int valuesCount = values.Count;
            int keysCount = keys.Count;


            if (valuesCount > 0 && keysCount > 0)
            {
                for (int i = 0; i < Math.Max(valuesCount, keysCount); i++)
                {
                    if (values[Math.Min(i, valuesCount - 1)] == null)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Null item - missing a value???");
                    }
                    locals.Add(keys[Math.Min(i, keysCount - 1)], values[Math.Min(i, valuesCount - 1)]);
                    outPairs.Add($"{("<" + keys[Math.Min(i, keysCount - 1)]).PadRight(keysLength + 1)}> --> {values[Math.Min(i, valuesCount - 1)]}");
                }

            }

            List<string> outputs = new List<string>(inputs.Count);

            inputs.ForEach(i => outputs.Add(i.AddLocals(locals)));

            DA.SetDataList(0, outputs);
            DA.SetDataList(1, outPairs);


        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9B21A68A-179E-4BDB-8533-0729E8CF5EA4"); }
        }
    }
}
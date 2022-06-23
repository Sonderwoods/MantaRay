using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_ApplyOverrides : GH_Template, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the GH_ApplyGlobals class.
        /// </summary>
        public GH_ApplyOverrides()
          : base("Apply Overrides", "Overrides",
              "Adds the overrides to the text element",
              "0 Setup")
        {
        }
        int staticParameterCount = 0;
        List<string> missingInputs = new List<string>();



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            staticParameterCount = this.Params.Input.Count;
            pManager.AddTextParameter("Input", "Input", "Input", GH_ParamAccess.list);
            pManager[pManager.AddTextParameter("Additional Keys", "Keys", "Keys", GH_ParamAccess.list, new List<string>())].Optional = true;
            pManager[pManager.AddTextParameter("Additional Values", "Values", "Values", GH_ParamAccess.list, new List<string>())].Optional = true;
            staticParameterCount = this.Params.Input.Count - staticParameterCount;
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
            missingInputs.Clear();


            if (keys.Count != values.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "List lengths are not matching");
            }

            int keysLength = 3;
            if (GlobalsHelper.Globals.Keys.Count > 0)
            {
                keysLength = GlobalsHelper.Globals.Keys.Select(k => k.Length).Max();
            }
            if (keys.Count > 0)
            {
                keysLength = Math.Max(keysLength, keys.Select(k => k.Length).Max());
            }
            if (staticParameterCount > 0)
            {
                keysLength = Math.Max(keysLength, Params.Input.Skip(staticParameterCount).Select(p => p.NickName.Length).Max());
            }
            keysLength += 2; // account for <>

            foreach (KeyValuePair<string, string> item in GlobalsHelper.Globals)
            {
                //outPairs.Add($"<{item.Key}> --> {item.Value}");
                outPairs.Add($"{("<" + item.Key + ">").PadRight(keysLength + 1)} --> {item.Value}");
            }



            if (keys.Count == 0 && values.Count == 0 && staticParameterCount == 0)
            {

                if (missingInputs.Count == 0)
                    DA.SetDataList(0, inputs.Select(s => s.AddGlobals(missingKeys: missingInputs)));

                DA.SetDataList(1, outPairs);

                foreach (string item in missingInputs)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Missing \"{item}\"");
                }

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


            foreach (Param_String input in this.Params.Input.Skip(staticParameterCount).OfType<Param_String>())
            {
                if (input.VolatileDataCount == 0)
                    continue;

                System.Collections.IList dataList = input.VolatileData.get_Branch(0);
                if (dataList.Count > 0 && dataList[0] is GH_String s)
                {
                    locals.Add(input.NickName, s.Value);
                    outPairs.Add($"{("<" + input.NickName + ">").PadRight(keysLength + 1)} --> {s.Value}");

                }

            }

            List<string> outputs = new List<string>(inputs.Count);

            inputs.ForEach(i => outputs.Add(i.AddGlobals(locals, missingKeys: missingInputs)));

            foreach (string item in missingInputs)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Missing \"{item}\"");

            }

            if (missingInputs.Count == 0)
                DA.SetDataList(0, outputs);

            DA.SetDataList(1, outPairs);

        }

        //protected override void ExpireDownStreamObjects()
        //{
        //    ////SMART BUT DANGEROUS!! Will only update downstream if the values are changed.. This is NOT intended grasshoppper behavior though!
        //    //if (missingInputs.Count == 0)
        //    //{
        //    //    if (oldOutputs.Count == Outputs.Count)
        //    //    {
        //    //        for (int i = 0; i < oldOutputs.Count; i++)
        //    //        {
        //    //            if (!oldOutputs[i].Equals(Outputs[i]))
        //    //            {
        //    //                base.ExpireDownStreamObjects();
        //    //                return;
        //    //            }
        //    //        }
        //    //    }
        //    //    else
        //    //    {
        //    //        base.ExpireDownStreamObjects();

        //    //    }


        //    //ALTERNATE METHOD. SOMEHOW THIS IS DELAYED SO IT HAPPENS 1 ITERATION TOO LATE
        //    if (missingInputs.Count == 0)
        //    {
        //        base.ExpireDownStreamObjects();
        //    }

        //}



        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Input && index >= staticParameterCount;
        }

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Input && index >= staticParameterCount;
        }

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {
            var param = new Param_String { NickName = "-" };
            return param;
        }

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
            for (var i = staticParameterCount; i < Params.Input.Count; i++)
            {
                var param = Params.Input[i];
                if (param.NickName == "-")
                {
                    param.Name = $"Data {i + 1}";
                    param.NickName = $"d{i + 1}";

                }
                else
                {
                    param.Name = param.NickName;
                }
                param.Description = $"Input {i + 1}";
                param.Optional = true;
                param.MutableNickName = true;
                param.Access = GH_ParamAccess.item;
                param.DataMapping = GH_DataMapping.Flatten;


            }

            this.Params.ParameterNickNameChanged -= Params_ParameterNickNameChanged;
            this.Params.ParameterNickNameChanged += Params_ParameterNickNameChanged;
        }

        private void Params_ParameterNickNameChanged(object sender, GH_ParamServerEventArgs e)
        {

            if (e.ParameterSide == GH_ParameterSide.Input)
            {
                int index = this.Params.Input.FindIndex(a => object.ReferenceEquals(a, e.Parameter));

                if (index >= staticParameterCount)
                {
                    this.ExpireSolution(true);
                }

            }
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9B21A68A-179E-4BDB-8232-0729E8CF5EA4"); }
        }
    }
}
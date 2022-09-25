using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace MantaRay.OldComponents
{
    [Obsolete]
    public class GH_ApplyOverrides_OBSOLETE : GH_Template, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the GH_ApplyGlobals class.
        /// </summary>
        public GH_ApplyOverrides_OBSOLETE()
          : base("Apply Overrides", "Overrides",
              "Adds the overrides to the text element\n" +
                "Will replace all <key> in the input with their corrosponding values.\n" +
                "IE: put '-n <cpus>' in the input, 'cpus' in the keys and '8' in the values. This will output '-n 8'\n\n" +
                "Use the zoomable UI to add additional key/value pairs. It'll use the input nickname as key and the input as value.\n\n" +
                "The values can be truncated, then use <filename-X> in the input. if <filename> is 'picture.hdr', then <filename-4> is 'picture'\n" +
                "The values can also remove any file extensions (anything after a dot), in that case use <filename-.>",
              "0 Setup")
        {
        }
        int staticParameterCount = 0;
        List<string> missingInputs = new List<string>();

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            staticParameterCount = Params.Input.Count;

            pManager.AddTextParameter("Input", "Input_", "Input string", GH_ParamAccess.list);
            pManager[pManager.AddTextParameter("Additional Keys", "_Keys", "Keys, list must match length of values list. Each key will be replaced ", GH_ParamAccess.list, new List<string>())].Optional = true;
            pManager[pManager.AddTextParameter("Additional Values", "_Values", "Values, list must match length of keys list.", GH_ParamAccess.list, new List<string>())].Optional = true;

            staticParameterCount = Params.Input.Count - staticParameterCount;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
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

            int dynamicParameterCount = Params.Input.Count - staticParameterCount;
            List<string> keys = DA.FetchList<string>("Additional Keys");
            List<string> values = DA.FetchList<string>("Additional Values");
            List<string> outPairs = new List<string>(keys.Count);
            List<string> inputs = DA.FetchList<string>("Input");
            missingInputs.Clear();


            if (keys.Count != values.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Keys and Value List lengths are not matching");
            }

            // House keeping to set the PadRight distance in the output (only aesthetic)
            int keysLength = 3;
            if (GlobalsHelper.Globals.Keys.Count > 0)
            {
                keysLength = GlobalsHelper.Globals.Keys.Select(k => k.Length).Max();
            }
            if (keys.Count > 0)
            {
                keysLength = Math.Max(keysLength, keys.Select(k => k.Length).Max());
            }
            if (dynamicParameterCount > 0)
            {
                keysLength = Math.Max(keysLength, Params.Input.Skip(staticParameterCount).Select(p => p.NickName.Length).Max());
            }
            keysLength += 2; // account for <>

            //foreach (KeyValuePair<string, string> item in GlobalsHelper.Globals)
            //{
            //    outPairs.Add($"{("<" + item.Key + ">").PadRight(keysLength + 1)} --> {item.Value}");
            //}


            // House keeping if list lengths are OK
            if (keys.Count == 0 && values.Count == 0 && dynamicParameterCount == 0)
            {

                if (missingInputs.Count == 0)
                {
                    DA.SetDataList(0, inputs.Select(s => s.ApplyGlobals(missingKeys: missingInputs)));
                }

                DA.SetDataList(1, outPairs);

                foreach (string item in missingInputs)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Missing \"{item}\"");
                }

                return;
            }


            Dictionary<string, string> locals = new Dictionary<string, string>(GlobalsHelper.Globals, StringComparer.OrdinalIgnoreCase);

            // Adding keys and values from the Key/Value list
            int valuesCount = values.Count;
            int keysCount = keys.Count;

            if (valuesCount > 0 && keysCount > 0)
            {
                for (int i = 0; i < Math.Max(valuesCount, keysCount); i++)
                {
                    if (values[Math.Min(i, valuesCount - 1)] == null)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Null item at key {keys[Math.Min(i, keysCount - 1)]} - missing a value???");

                    }
                    if (locals.ContainsKey(keys[Math.Min(i, keysCount - 1)]))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Key {keys[Math.Min(i, keysCount - 1)]} (in the Keys list) already exists. Overwritten to {values[Math.Min(i, valuesCount - 1)]}");
                    }

                    locals[keys[Math.Min(i, keysCount - 1)]] = values[Math.Min(i, valuesCount - 1)];
                }

            }

            // Adding keys and values from the dynamic parameter inputs
            foreach (Param_String input in Params.Input
                .Skip(staticParameterCount)
                .OfType<Param_String>()
                .Where(inp => inp.VolatileDataCount > 0))
            {
                int branchIndex = input.VolatileData.PathCount == 1 ? 0 : RunCount - 1;

                if (input.VolatileData.PathCount <= branchIndex)
                    throw new ArgumentOutOfRangeException(input.NickName);


                System.Collections.IList dataList = input.VolatileData.get_Branch(branchIndex);

                if (dataList.Count > 0 && dataList[0] is GH_String s)
                {
                    if (locals.ContainsKey(input.NickName))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Key {input.NickName} (in the dynamic parameters) already exists. Overwritten to {s.Value}");
                    }


                    locals[input.NickName] = s.Value;

                }

            }

            //for (int i = 2; i < Params.Input.Count; i++)
            //{
            //    string key = Params.Input[i].NickName;
            //    string value = DA.Fetch<string>(i);

            //    if (locals.ContainsKey(key))
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Key {key} (in the dynamic parameters) already exists. Overwritten to {value}");
            //    }


            //    locals[key] = value;
            //}

            foreach (KeyValuePair<string, string> item in locals.OrderBy(o => o.Key))
            {
                outPairs.Add($"{("<" + item.Key + ">").PadRight(keysLength + 1)} --> {item.Value}");
            }

            //as array instead of IEnumerable, otherwise the misingInputs is not updated before the check below.
            string[] outputs = inputs.Select(i => i.ApplyGlobals(locals, missingKeys: missingInputs)).ToArray();

            //extra round to fix nested keys (inside a value)!
            outputs = outputs.Select(i => i.ApplyGlobals(locals, missingKeys: missingInputs)).ToArray();

            missingInputs.ForEach(item => AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Missing \"{item}\""));

            if (RuntimeMessages(GH_RuntimeMessageLevel.Error).Count == 0)
            {
                DA.SetDataList(0, outputs);
            }

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
        public void AddMissingParameters()
        {
            foreach (string missingInp in missingInputs
                .Distinct()
                .Where(i => !Params.Input.Select(ip => ip.NickName).Contains(i)))
            {
                IGH_Param param = new Param_String() { NickName = missingInp, Optional = true, Access = GH_ParamAccess.item };
                Params.RegisterInputParam(param);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);

        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Fill missing parameters", (s, e) =>
            {
                AddMissingParameters();
            }, missingInputs.Count > 0 && missingInputs
                .Distinct()
                .Where(i => !Params.Input.Select(ip => ip.NickName).Contains(i)).Count() > 0);
            Menu_AppendItem(menu, "Remove unneeded parameters (TODO)", (s, e) =>
            {
                //AddMissingParameters();
            }, false);

        }


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
            int added = 0;
            for (var i = staticParameterCount; i < Params.Input.Count; i++)
            {
                var param = Params.Input[i];

                if (param.NickName == "-")
                {
                    param.NickName = missingInputs.Skip(staticParameterCount + added++).FirstOrDefault() ?? $"d{i + 1}";
                }

                param.Name = param.NickName;
                param.Access = GH_ParamAccess.item;
                param.Description = $"Input {i + 1}";
                param.Optional = true;
                param.MutableNickName = true;

                param.DataMapping = GH_DataMapping.Graft;

            }

            Params.ParameterNickNameChanged -= Params_ParameterNickNameChanged;
            Params.ParameterNickNameChanged += Params_ParameterNickNameChanged;
        }

        private void Params_ParameterNickNameChanged(object sender, GH_ParamServerEventArgs e)
        {

            if (e.ParameterSide == GH_ParameterSide.Input)
            {
                int index = Params.Input.FindIndex(a => ReferenceEquals(a, e.Parameter));

                if (index >= staticParameterCount)
                {
                    ExpireSolution(true);
                }

            }
        }

        protected override Bitmap Icon => Resources.Resources.Ra_Globals_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9B21A68A-179E-4BDB-8232-0729E8CF5EA4"); }

        }
    }
}
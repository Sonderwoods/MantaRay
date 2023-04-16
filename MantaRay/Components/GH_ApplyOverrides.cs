using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using MantaRay.Types;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_ApplyOverrides : GH_Template, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the GH_ApplyGlobals class.
        /// </summary>
        public GH_ApplyOverrides()
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

        public override bool IsPreviewCapable => false;



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            staticParameterCount = this.Params.Input.Count;

            pManager.AddGenericParameter("Input", "Input", "Input string", GH_ParamAccess.list);

            staticParameterCount = this.Params.Input.Count - staticParameterCount;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Output", "Output with globals applied.\nOutputs a text object", GH_ParamAccess.list);
            pManager.AddGenericParameter("DynamicOutput", "DynamicOutput", "output with globals 'on demand'\nUse me to connect to Execute Component\n\n" +
                "This is NOT your average text object, because if you feed it into the Execute Component,\n" +
                "the globals will not be applied untill the Execute Component is actually executing.\n\n" +
                "This gives some advantages in case some globals were changed such as project folder etc.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (PrincipalParameterIndex < 0)
                PrincipalParameterIndex = 0;

            int dynamicParameterCount = Params.Input.Count - staticParameterCount;
            List<OverridableText> inputs = DA.FetchList<OverridableText>(this, "Input").Select(o => (OverridableText)o.Duplicate()).ToList();
            missingInputs.Clear();


            // House keeping to set the PadRight distance in the output (only aesthetic)
            int keysLength = 3;
            if (GlobalsHelper.Globals.Keys.Count > 0)
            {
                keysLength = GlobalsHelper.Globals.Keys.Select(k => k.Length).Max();
            }

            if (dynamicParameterCount > 0)
            {
                keysLength = Math.Max(keysLength, Params.Input.Skip(staticParameterCount).Select(p => p.NickName.Length).Max());
            }
            keysLength += 2; // account for <>



            Dictionary<string, string> localsTest = new Dictionary<string, string>(GlobalsHelper.Globals, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> locals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


            // Adding keys and values from the dynamic parameter inputs
            foreach (Param_String input in this.Params.Input
                .Skip(staticParameterCount)
                .OfType<Param_String>()
                .Where(inp => inp.VolatileDataCount > 0))
            {
                int branchIndex = input.VolatileData.PathCount == 1 ? 0 : this.RunCount - 1;

                if (input.VolatileData.PathCount <= branchIndex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"issues with {input.NickName}");
                    continue;
                }

                System.Collections.IList dataList = input.VolatileData.get_Branch(branchIndex);

                if (dataList.Count > 0 && dataList[0] is GH_String s)
                {
                    if (localsTest.ContainsKey(input.NickName))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Key {input.NickName} (in the dynamic parameters) already exists. Overwritten to {s.Value}");
                    }


                    localsTest[input.NickName] = s.Value;
                    locals[input.NickName] = s.Value;

                }

            }

            foreach (var @input in inputs)
            {
                input.Locals = locals;
            }


            //ToArray to make sure it's enumerated and thus added to missing inputs.
            OverridableText[] outputs = inputs.Select(i => new OverridableText(i, locals, missingInputs)).ToArray();
            string[] outputsRaw = inputs.Select(i => new OverridableText(i, locals, missingInputs).Value).ToArray();

            if (missingInputs.Any())
            {

                AddMissingParameters();

            }

            missingInputs.ForEach(item => AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Missing \"{item}\""));

            if (RuntimeMessages(GH_RuntimeMessageLevel.Error).Count == 0)
            {
                DA.SetDataList(0, outputsRaw);

                if (Params.Output.Count == 2)
                {
                    DA.SetDataList(1, outputs);
                }
            }




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
            bool changed = false;
            foreach (string missingInp in missingInputs
                .Distinct()
                .Where(i => !Params.Input.Select(ip => ip.NickName).Contains(i)))
            {
                IGH_Param param = new Param_String() { NickName = missingInp, Optional = true, Access = GH_ParamAccess.item, DataMapping = GH_DataMapping.Graft };
                Params.RegisterInputParam(param);
                changed = true;
            }
            if (changed)
            {

                Params.OnParametersChanged();
                this.ExpireSolution(true);
            }

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
            return (side == GH_ParameterSide.Input && index >= staticParameterCount) ||
                (side == GH_ParameterSide.Output && index == 1 && Params.Output.Count == 1);
        }

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return (side == GH_ParameterSide.Input && index >= staticParameterCount) ||
                (side == GH_ParameterSide.Output && index == 1);
        }

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input)
            {
                var param = new Param_String { NickName = "-" };
                return param;
            }
            else
            {
                var param = new Param_GenericObject { NickName = "-" };
                return param;
            }

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
                    param.NickName = missingInputs.Where(n => Params.IndexOfInputParam(n) < 0).Distinct().Skip(added++).FirstOrDefault() ?? $"d{i + 1}";
                }

                param.Name = param.NickName;
                param.Access = GH_ParamAccess.item;
                param.Description = $"Input {i + 1}";
                param.Optional = true;
                param.MutableNickName = true;

                param.DataMapping = GH_DataMapping.Graft;

            }

            if (Params.Output.Count == 2)
            {
                var param = Params.Output[1];
                if (param.NickName == "-")
                {
                    param.NickName = "DynamicOutput";
                }
            }

            this.Params.ParameterNickNameChanged -= Params_ParameterNickNameChanged;
            this.Params.ParameterNickNameChanged += Params_ParameterNickNameChanged;
        }

        private void Params_ParameterNickNameChanged(object sender, GH_ParamServerEventArgs e)
        {

            if (e.ParameterSide == GH_ParameterSide.Input)
            {
                int index = this.Params.Input.FindIndex(a => ReferenceEquals(a, e.Parameter));

                if (index >= staticParameterCount)
                {
                    this.ExpireSolution(true);
                }

            }
        }

        protected override Bitmap Icon => Resources.Resources.Ra_Globals_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9B21A68A-179E-4BDB-8232-0722E8AF5EA4"); }

        }
    }
}
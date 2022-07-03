using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_Globals : GH_Template, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the GH_Globals class.
        /// </summary>
        public GH_Globals()
          : base("Global Overrides", "Global Overrides",
              "Sets globals that can be replaced in the ssh commands and in paths",
              "0 Setup")
        {
        }

        int staticParameterCount = 0;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            staticParameterCount = this.Params.Input.Count;

            pManager[pManager.AddTextParameter("Keys", "Keys", "Keys", GH_ParamAccess.list)].Optional = true;
            pManager[pManager.AddTextParameter("Values", "Values", "Values", GH_ParamAccess.list)].Optional = true;

            staticParameterCount = this.Params.Input.Count - staticParameterCount;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Pairs", "Pairs", "Pairs", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Moving to back will make sure this expires/runs before other objects when you load the file
            Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(this, GH_Arrange.MoveToBack);

            if (Grasshopper.Instances.ActiveCanvas.Document.Objects
                .OfType<GH_Globals>()
                .Where(c => !Object.ReferenceEquals(c, this) && !c.Locked)
                .Count() > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"There's more than one {this.NickName} component on the canvas.\n" +
                    $"One will override the other!\n" +
                    $"Please only use ONE! Do you get it???\n" +
                    $"For one to live the other one has to die\n" +
                    $"It's like Harry Potter and Voldemort.\n\nDisable the other component and enable this one again. Fool.");

                if (Grasshopper.Instances.ActiveCanvas.Document.Objects.
                    OfType<GH_Globals>().
                    Where(c => c.Locked != true).
                    Where(c => !Object.ReferenceEquals(c, this)).
                    Count() > 0)
                {
                    this.Locked = true;
                    return;

                }

            }
            int dynamicParameterCount = Params.Input.Count - staticParameterCount;


            List<string> keys = DA.FetchList<string>("Keys");
            List<string> values = DA.FetchList<string>("Values");
            List<string> outPairs = new List<string>(keys.Count);

            if (keys.Count != values.Count) throw new ArgumentOutOfRangeException("The list lengths do not match in Keys/Values inputs");

            GlobalsHelper.Globals.Clear();
            GlobalsHelper.Globals["WinHome"] = SSH_Helper.WindowsFullpath;
            GlobalsHelper.Globals["LinuxHome"] = SSH_Helper.LinuxFullpath;
            GlobalsHelper.Globals["cpus"] = (Environment.ProcessorCount - 1).ToString();


            for (int i = 0; i < keys.Count; i++)
            {
                GlobalsHelper.Globals[keys[i]] = values[i];
            }

            // Adding keys and values from the dynamic parameter inputs
            foreach (Param_String input in this.Params.Input
                .Skip(staticParameterCount)
                .OfType<Param_String>()
                .Where(inp => inp.VolatileDataCount > 0))
            {


                System.Collections.IList dataList = input.VolatileData.get_Branch(this.RunCount - 1);
                if (dataList.Count > 0 && dataList[0] is GH_String s)
                {
                    if (GlobalsHelper.Globals.ContainsKey(input.NickName))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Key {input.NickName} (in the dynamic parameters) already exists and is now overwritten");
                    }

                    GlobalsHelper.Globals[input.NickName] = s.Value;

                }

            }

            int keysLength = 3;
            if (GlobalsHelper.Globals.Keys.Count > 0)
            {
                keysLength = GlobalsHelper.Globals.Keys.Select(k => k.Length).Max();
            }


            // Outputs
            foreach (KeyValuePair<string, string> item in GlobalsHelper.Globals)
            {
                outPairs.Add($"{("<" + item.Key + ">").PadRight(keysLength + 2)} --> {item.Value}");
            }

            DA.SetDataList(0, outPairs);

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

            for (var i = staticParameterCount; i < Params.Input.Count; i++)
            {
                var param = Params.Input[i];

                if (param.NickName == "-")
                {
                    param.NickName = $"d{i + 1}";
                }

                param.Name = param.NickName;
                param.Access = GH_ParamAccess.item;
                param.Description = $"Input {i + 1}";
                param.Optional = true;
                param.MutableNickName = true;

                //param.DataMapping = GH_DataMapping.Flatten;

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

        public override void RemovedFromDocument(GH_Document document)
        {
            if (!Locked)
            {
                GH_Globals otherGlobal = document.Objects
                .OfType<GH_Globals>()
                .Where(o => !ReferenceEquals(o, this))
                .FirstOrDefault();

                if (otherGlobal != null)
                {
                    otherGlobal.Locked = false;
                    otherGlobal.ExpireSolution(true);
                }
            }

        }

        protected override Bitmap Icon => Resources.Resources.Ra_Globals_Icon2;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A79EFEF6-AFDB-4A5F-8955-A3C51BCF7CE0"); }
        }
    }
}
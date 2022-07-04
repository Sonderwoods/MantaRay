using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrasshopperRadianceLinuxConnector
{
    public abstract class GH_Template_SaveStrings : GH_Template
    {

        protected string[] OldResults = new string[0];
        protected bool Running = false;


        protected GH_Template_SaveStrings(string name, string nickname, string description, string subcategory = "Test")
            : base(name, nickname, description, subcategory)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DA"></param>
        /// <param name="stringOutput">index of the output param of the saved string</param>
        /// <returns></returns>
        public bool CheckIfRunOrUseOldResults<T>(IGH_DataAccess DA, int stringOutput, IEnumerable<T> objs = null)
        {
            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>("Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>("Run"))
            {
                
                if (objs != null && objs.Count() > 0)
                {
                    Message = "Reusing results";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old result\nThis can be convenient for opening old workflows and not running everything again.");
                    DA.SetDataList(stringOutput, objs);
                }
                this.Hidden = true;
                Running = false;
                return false;

            }
            Running = true;
            this.Hidden = false;
            Message = "";
            return true;
        }

        public bool CheckIfRunOrUseOldResults(IGH_DataAccess DA, int stringOutput)
        {
            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>("Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>("Run"))
            {
                if (OldResults != null && OldResults.Length > 0)
                {
                    Message = "Reusing results";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old result\nThis can be convenient for opening old workflows and not running everything again.");
                    DA.SetDataList(stringOutput, OldResults);
                }

                this.Hidden = true;
                Running = false;
                return false;

            }
            Running = true;
            this.Hidden = false;
            Message = "";
            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            string s = String.Empty;
            if (reader.TryGetString("stdouts", ref s))
            {
                OldResults = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None);
            }

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("stdouts", String.Join(">JOIN<", OldResults));

            return base.Write(writer);
        }

        public override bool IsPreviewCapable => true;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendItem(menu, "Clear cached stdout", (s, e) => { OldResults = new string[0]; ExpireSolution(true); })
                .ToolTipText = "Removes the data saved in the component.";
        }
    }
}

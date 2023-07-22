using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MantaRay.Components.Templates
{
    public abstract class GH_Template_SaveStrings : GH_Template, IClearData
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
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>(this, "Run"))
            {

                if (objs != null && objs.Count() > 0)
                {
                    Message = "Reusing results";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old result\nThis can be convenient for opening old workflows and not running everything again.");
                    DA.SetDataList(stringOutput, objs);
                }
                Hidden = true;
                Running = false;
                return false;

            }
            Running = true;
            Hidden = false;
            Message = "";
            return true;
        }

        public bool CheckIfRunOrUseOldResults(IGH_DataAccess DA, int stringOutput, bool limitToFirstRun = false)
        {
            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>(this, "Run"))
            {
                if (OldResults != null && OldResults.Length > 0 && !limitToFirstRun || RunCount == 1)
                {
                    Message = "Reusing results";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old result\nThis can be convenient for opening old workflows and not running everything again.");
                    DA.SetDataList(stringOutput, OldResults);
                }

                Hidden = true;
                Running = false;
                return false;

            }
            Running = true;
            Hidden = false;
            Message = "";
            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            string s = string.Empty;
            if (reader.TryGetString("stdouts", ref s))
            {
                OldResults = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None);
            }

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("stdouts", string.Join(">JOIN<", OldResults));

            return base.Write(writer);
        }

        public override bool IsPreviewCapable => true;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendItem(menu, "Clear cached stdout", (s, e) => { ClearCachedData(); ExpireSolution(true); })
                .ToolTipText = "Removes the data saved in the component.";
            Menu_AppendItem(menu, "Clear cached data in ALL components", (s, e) => { ClearAllCachedData(); ExpireSolution(true); })
                .ToolTipText = "Removes the data saved in the component.";
        }

        /// <summary>
        /// Here we reset all persistant data such as results, old runtimes etc.
        /// </summary>
        public virtual void ClearCachedData()
        {
            OldResults = new string[0];
        }

        public static void ClearAllCachedData()
        {

            var mb = MessageBox.Show("Clear cached strings in all objects,\n" +
                "including upload component, execute component and others.\n\nThis will recompute your entire document.", "Are you sure?", MessageBoxButtons.YesNo);
            if (mb == DialogResult.Yes)
            {

                foreach (IClearData obj in Grasshopper.Instances.ActiveCanvas.Document.Objects.OfType<IClearData>())
                {
                    obj.ClearCachedData();
                }
                Grasshopper.Instances.ActiveCanvas.Document.ExpireSolution();
            }

        }

    }
}

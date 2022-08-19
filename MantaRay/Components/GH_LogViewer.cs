using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using MantaRay.Components;
using MantaRay.Components.Templates;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_LogViewer : GH_Template, IHasDoubleClick
    {
        /// <summary>
        /// Initializes a new instance of the GH_LogViewer class.
        /// </summary>
        public GH_LogViewer()
          : base("LogViewer", "LogViewer",
              "Auto update log viewer. Output only to a panel",
              "0 Setup")
        {
        }

        LogHelper logHelper;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("Name", "Name", "Name", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddIntegerParameter("Number", "Number", "Number", GH_ParamAccess.item, 10)].Optional = true;
            pManager[pManager.AddTextParameter("NameFilter", "NameFilter", "NameFilter", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("DescFilter", "DescFilter", "Description filter (typically the commands etc)", GH_ParamAccess.item, "")].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CurrentTasks", "CurrentTasks", "CurrentTasks", GH_ParamAccess.list);
            pManager.AddTextParameter("Logs", "Logs", "Logs", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Moving to back will make sure this expires/runs after other objects when you load the file
            Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(this, GH_Arrange.MoveToFront);

            UnsubscribeAll(); // in case name was changed - we unsubscribe everything first.

            logHelper = LogHelper.GetLogHelper(DA.Fetch<string>("Name"));

            logHelper.LogUpdated += LogHelper_LogUpdated;

            DA.SetDataList(0, logHelper.GetCurrentTasks(DA.Fetch<int>("Number"), DA.Fetch<string>("NameFilter"), DA.Fetch<string>("DescFilter")));
            DA.SetDataList(1, logHelper.GetLatestLogs(DA.Fetch<int>("Number"), DA.Fetch<string>("NameFilter"), DA.Fetch<string>("DescFilter")));
        }


        protected override void BeforeSolveInstance()
        {
            if (logHelper != null)
            {
                logHelper.LogUpdated -= LogHelper_LogUpdated;
                logHelper.LogUpdated += LogHelper_LogUpdated;
            }
        }

        public void UnsubscribeAll()
        {
            foreach (KeyValuePair<string, LogHelper> item in LogHelper.AllLogSystems)
            {
                LogHelper h = item.Value;
                h.LogUpdated -= LogHelper_LogUpdated;
            }
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {

            if (logHelper != null)
            {

                if (context == GH_DocumentContext.Loaded)
                {
                    logHelper.LogUpdated -= LogHelper_LogUpdated;
                    logHelper.LogUpdated += LogHelper_LogUpdated;

                }
                else
                {
                    logHelper.LogUpdated -= LogHelper_LogUpdated;
                }
            }
            base.DocumentContextChanged(document, context);

        }

        public override void RemovedFromDocument(GH_Document document)
        {

            logHelper.LogUpdated -= LogHelper_LogUpdated;
            base.RemovedFromDocument(document);

        }

        

        private void LogHelper_LogUpdated(object sender, EventArgs e)
        {
            if (((LogHelper)sender).Name == logHelper.Name)
            {
                Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(5, x => this.ExpireSolution(true));
            }
            //this.ExpireSolution(true);
            else
                ((LogHelper)sender).LogUpdated -= LogHelper_LogUpdated; //in case we missed some unsubscribtions
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
 
            Menu_AppendItem(menu, "Clear Log", (s, e) => { ClearLog(); }, true);
        
        }

        private void ClearLog()
        {
            logHelper.CLear();
        }

        public override bool Read(GH_IReader reader)
        {

            //TODO

            //string s = String.Empty;

            //if (reader.TryGetString("stdouts", ref s))
            //{
            //    string[] splitString = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None);

            //    savedResults = new RunInfo[splitString.Length];

            //    for (int i = 0; i < splitString.Length; i++)
            //    {
            //        savedResults[i] = new RunInfo() { Stdout = new StringBuilder(splitString[i]) };
            //    }
            //}

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {

            //writer.SetString("stdouts", String.Join(">JOIN<", savedResults.Select(r => r.Stdout)));

            return base.Write(writer);
        }

        public GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            this.ExpireSolution(true);
            return GH_ObjectResponse.Handled;
        }

        public override void CreateAttributes()
        {
            //base.CreateAttributes();
            m_attributes = new GH_DoubleClickAttributes(this);

        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A151B41B-5C0D-4CA6-9F7A-FD67A6F565E1"); }
        }
    }
}
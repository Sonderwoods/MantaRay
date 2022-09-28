using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_Download : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_Download class.
        /// </summary>
        public GH_Download()
          : base("Download", "Download",
              "Download a file from linux to local in windows",
              "1 SSH")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Linux File Paths", "Linux File", "Linux file path", GH_ParamAccess.list);
            pManager[pManager.AddTextParameter("Target local folder", "target folder", "Local target folder in windows", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("File Paths", "File Paths", "Path to the files", GH_ParamAccess.list);
            pManager.AddTextParameter("Status", "Status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("Ran", "Ran", "Run", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!CheckIfRunOrUseOldResults(DA, 0)) return; //template


            string targetFolder = DA.Fetch<string>(this, "Target local folder");

            if (targetFolder == null || String.IsNullOrEmpty(targetFolder))
            {
                targetFolder = SSH_Helper.WindowsFullpath;
            }

            string localTargetFolder = targetFolder; // Path.GetDirectoryName(targetFolder);

            List<string> allFilePaths = DA.FetchList<string>(this, "Linux File Paths");
            List<string> localFilePaths = new List<string>(allFilePaths.Count);

            StringBuilder sb = new StringBuilder();

            foreach (var file in allFilePaths)
            {
                SSH_Helper.Download(file, localTargetFolder, sb);
                localFilePaths.Add(localTargetFolder + "\\" + Path.GetFileName(file));
            }

            OldResults = localFilePaths.ToArray();


            DA.SetDataList("File Paths", localFilePaths);
            DA.SetData("Status", sb.ToString());
        }



        protected override Bitmap Icon => Resources.Resources.Ra_Download_Icon;




        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6C897E25-1EBE-48D8-AD3A-121DA1B2AD90"); }
        }
    }
}
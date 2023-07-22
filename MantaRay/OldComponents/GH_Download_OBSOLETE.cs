using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay.OldComponents
{
    [Obsolete]
    public class GH_Download_OBSOLETE : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_Download class.
        /// </summary>
        public GH_Download_OBSOLETE()
          : base("Download", "Download",
              "Download a file from linux to local in windows",
              "1 SSH")
        {
        }


        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Linux File Paths", "Linux File", "Linux file path", GH_ParamAccess.list);
            pManager[pManager.AddTextParameter("Target local folder", "target folder", "Local target folder in windows", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("File Paths", "File Paths", "Path to the files", GH_ParamAccess.list);
            pManager.AddTextParameter("Ran", "Ran", "Run", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!CheckIfRunOrUseOldResults(DA, 1)) return; //template

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());


            string targetFolder = DA.Fetch<string>(this, "Target local folder");

            if (targetFolder == null || string.IsNullOrEmpty(targetFolder))
            {
                targetFolder = sshHelper.WinHome;
            }

            string localTargetFolder = targetFolder; // Path.GetDirectoryName(targetFolder);

            List<string> allFilePaths = DA.FetchList<string>(this, "Linux File Paths");
            List<string> localFilePaths = new List<string>(allFilePaths.Count);

            StringBuilder sb = new StringBuilder();

            foreach (var file in allFilePaths)
            {
                sshHelper.Download(file, localTargetFolder, sb);
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
            get { return new Guid("6C897E25-1EBE-48D8-AD3A-111DB1B2AD90"); }
        }
    }
}
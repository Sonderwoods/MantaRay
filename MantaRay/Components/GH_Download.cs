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
using MantaRay.Components.Templates;
using MantaRay.Helpers;
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
            pManager.AddTextParameter("SftpPaths", "SftpPaths", "Linux file path\nsuch as ~/mystuff/ or local paths inside your project folder such as radFiles/materials.mat\nor in case you are on windows SFTP it will be a windows path\n" +
                "however that window path will be in a weird shape such as:\n" +
                "'/C:/Users/MyUsername'", GH_ParamAccess.list);
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

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());
            if (sshHelper == null || sshHelper.SftpClient == null || !sshHelper.SftpClient.IsConnected)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No connection");
                return;
            }


            string targetFolder = DA.Fetch<string>(this, "Target local folder");

            if (targetFolder == null || String.IsNullOrEmpty(targetFolder))
            {
                targetFolder = sshHelper.WinHome;
            }

            string localTargetFolder = targetFolder.ApplyGlobals(); // Path.GetDirectoryName(targetFolder);

            List<string> allFilePaths = DA.FetchList<string>(this, "SftpPaths", "Linux File Paths");
            List<string> localFilePaths = new List<string>(allFilePaths.Count);

            StringBuilder sb = new StringBuilder();

            foreach (var file in allFilePaths)
            {
                var file2 = file.Trim('\r', '\n');
                try
                {
                sshHelper.Download(file2, localTargetFolder, sb);
                localFilePaths.Add(localTargetFolder + "\\" + Path.GetFileName(file2));

                }
                catch (FileNotFoundException e)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
                    localFilePaths.Add(null);
                }
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
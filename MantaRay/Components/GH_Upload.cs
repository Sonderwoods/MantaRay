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
    public class GH_Upload : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_Upload class.
        /// </summary>
        public GH_Upload()
          : base("Upload", "Upload",
              "Upload a file through sFTP (through your SSH connection)",
              "1 SSH")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Local File Paths", "File", "File path", GH_ParamAccess.list);
            pManager[pManager.AddTextParameter("Subfolder Override", "Subfolder Override", "Subfolder Override\n" +
                "Example:\n" +
                "simulation/radFiles", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("File Paths", "File Paths", "Path to the files", GH_ParamAccess.list);
            pManager.AddTextParameter("Ran", "Ran", "Ran", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!CheckIfRunOrUseOldResults(DA, 1)) return; //template

            List<string> allFilePaths = DA.FetchList<string>("Local File Paths");

            List<string> outFilePaths = new List<string>(allFilePaths.Count);

            string subfolderOverride = DA.Fetch<string>("Subfolder Override").Replace('\\', '/').Trim('/');

            StringBuilder sb = new StringBuilder();

            string linuxPath = string.IsNullOrEmpty(subfolderOverride) ? SSH_Helper.LinuxFullpath : SSH_Helper.LinuxParentPath + "/" + subfolderOverride;

            for (int i = 0; i < allFilePaths.Count; i++)
            {
                try
                {
                    SSH_Helper.Upload(allFilePaths[i], linuxPath, sb);
                    outFilePaths.Add($"{linuxPath}/{Path.GetFileName(allFilePaths[i])}");

                }
                catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                {
                    sb.AppendFormat("Could not upload files - Path not found ({0})! {1}", linuxPath, e.Message);
                    break;
                }

            }

            OldResults = outFilePaths.ToArray();
            DA.SetDataList("File Paths", outFilePaths);
            DA.SetData("Status", sb.ToString());


        }

        protected override Bitmap Icon => Resources.Resources.Ra_Upload_Icon;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("338F971A-2BE9-4410-886F-ECDC841E5B91"); }
        }
    }
}
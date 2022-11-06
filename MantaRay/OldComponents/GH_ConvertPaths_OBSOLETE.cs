using System;
using System.Collections.Generic;

using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using MantaRay.Helpers;

namespace MantaRay.OldComponents
{
    [Obsolete]
    public class GH_ConvertPaths_OBSOLETE : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_ToLinux class.
        /// </summary>
        public GH_ConvertPaths_OBSOLETE()
          : base("ConvertPaths", "ConvertPaths",
              "converts a windows path to linux path",
              "0 Setup")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden; 

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("I", "I", "Input linux/windows path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Linux", "L", "Linux", GH_ParamAccess.item);
            pManager.AddTextParameter("Windows", "W", "Windows", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = DA.Fetch<string>(this, 0);

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());

            if (sshHelper != null && sshHelper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Connection");
                return;
            }
            DA.SetData(0, path.ToLinuxPath());
            DA.SetData(1, path.ToWindowsPath());
        }

        protected override Bitmap Icon => Resources.Resources.Ra_Paths_Icon;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("AF816E54-D307-4E15-9FDD-3DD9A0DC61ED"); }
        }
    }
}
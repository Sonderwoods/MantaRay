using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Grasshopper.Kernel;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using Rhino.Geometry;

namespace MantaRay.OldComponents
{
    [Obsolete]
    public class GH_TestConnection : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_TestConnection class.
        /// </summary>
        public GH_TestConnection()
          : base("TestConnection", "TestConnection",
              "Test connection",
              "0 Setup")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("status", "status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("errors", "errors", "errors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());
            StringBuilder sb = new StringBuilder();

            List<string> errors = new List<string>();
            try
            {
                sshHelper.Execute("cd ~ && ls -lah | head", stdout: sb);
                sshHelper.Execute("pwd", stdout: sb);
            }
            catch (Renci.SshNet.Common.SshConnectionException e)
            {
                sb.Append(e.Message);
            }



            DA.SetData("status", sb.ToString());
        }

        protected override Bitmap Icon => Resources.Resources.Ra_Connect_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("76F064E6-AF97-49F4-856B-05521601AEF2"); }
        }
    }
}
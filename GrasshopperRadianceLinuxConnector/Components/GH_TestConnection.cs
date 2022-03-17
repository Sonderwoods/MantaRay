using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_TestConnection : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_TestConnection class.
        /// </summary>
        public GH_TestConnection()
          : base("TestConnection", "TestConnection",
              "Test connection",
              "Test")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("status", "status", "status", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (SSH_Helper.SshClient != null && SSH_Helper.SshClient.IsConnected)
            {
                var cl = SSH_Helper.SshClient;

                StringBuilder sb = new StringBuilder();



                // quick way to use ist, but not best practice - SshCommand is not Disposed, ExitStatus not checked...
                sb.AppendLine(cl.CreateCommand("cd ~ && ls -lah").Execute());
                sb.AppendLine(cl.CreateCommand("pwd").Execute());
                //sb.AppendLine(cl.CreateCommand("cd /tmp/uploadtest && ls -lah").Execute());



                DA.SetData("status", sb.ToString());

            }
            else if (SSH_Helper.SshClient != null)
            {
                DA.SetData("status", "there is a client but no connection");
            }
            else
            {
                DA.SetData("status", "no client");
            }
        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("76F064E6-AF97-49F4-856B-05521601AEF2"); }
        }
    }
}
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_ObjToRad : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_ObjToRad class.
        /// </summary>
        public GH_ObjToRad()
          : base("ObjToRad", "Obj2Rad",
              "1) Copies your local obj files to the linux drive through SSH\n" +
                "2) Runs the obj2rad command and uses the -m argument for parsing the mapping file",
              "Rad")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("obj file paths", "obj file paths", "", GH_ParamAccess.list);
            pManager.AddTextParameter("map file", "map file", "mapping file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("status", "status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("rad file path", "rad file path", "rad file path", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (SSH_Helper.Client != null && SSH_Helper.Client.IsConnected)
            {
                using (var cl = SSH_Helper.Client)
                {
                    StringBuilder sb = new StringBuilder();



                    // quick way to use ist, but not best practice - SshCommand is not Disposed, ExitStatus not checked...
                    sb.AppendLine(cl.CreateCommand("cd ~ && ls -lah").Execute());
                    sb.AppendLine(cl.CreateCommand("pwd").Execute());
                    //sb.AppendLine(cl.CreateCommand("cd /tmp/uploadtest && ls -lah").Execute());



                    DA.SetData("status", sb.ToString());
                }
            }
            else if (SSH_Helper.Client != null)
            {
                DA.SetData("status", "there is a client but no connection");
            }
            else
            {
                DA.SetData("status", "no client");
            }


            // Upload A File
            using (var sftp = new SftpClient(ConnNfo))
            {
                string uploadfn = "Renci.SshNet.dll";

                sftp.Connect();
                sftp.ChangeDirectory("/tmp/uploadtest");
                using (var uplfileStream = System.IO.File.OpenRead(uploadfn))
                {
                    sftp.UploadFile(uplfileStream, uploadfn, true);
                }
                sftp.Disconnect();
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2832A54F-8FA4-45EB-ACE8-CC7F09BFA930"); }
        }
    }
}
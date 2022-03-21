using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Renci.SshNet;
using System.IO;

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
              "Geo")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("obj file paths", "obj file paths", "", GH_ParamAccess.list);
            pManager.AddTextParameter("map file", "map file", "mapping file", GH_ParamAccess.item);
            pManager[pManager.AddTextParameter("linux target folder", "linux target folder", "", GH_ParamAccess.item, "~")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("status", "status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("rad file paths", "rad file paths", "rad file paths", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.Fetch<bool>("Run"))
                return;


            List<string> allFilePaths = DA.FetchList<string>("obj file paths");
            
            allFilePaths.Add(DA.Fetch<string>("map file"));
            allFilePaths.Reverse(); //to make sure the map comes first in the upload process.

            List<string> radFilePaths = new List<string>(allFilePaths.Count);

            string sshPath = DA.Fetch<string>("linux target folder");

            StringBuilder sb = new StringBuilder();




            //SSH_Helper.Execute("cd ~ && ls -lah", sb);
            //SSH_Helper.Execute("pwd", sb);

            // quick way to use ist, but not best practice - SshCommand is not Disposed, ExitStatus not checked...
            //sb.AppendLine(cl.CreateCommand("cd ~ && ls -lah").Execute());
            //sb.AppendLine(cl.CreateCommand("pwd").Execute());
            //sb.AppendLine(cl.CreateCommand("cd /tmp/uploadtest && ls -lah").Execute());

            SSH_Helper.Execute($"pwd", sb);


            for (int i = 0; i < allFilePaths.Count; i++)
            {
                try
                {
                SSH_Helper.Upload(allFilePaths[i], sshPath, sb);

                }
                catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                {
                    sb.AppendFormat("Could not upload files - Path not found ({0})! {1}", sshPath, e.Message);
                    break;
                }
                

                if (i > 0) // skipping a command at the map file
                {
                    string radFilePath = System.IO.Path.GetFileNameWithoutExtension(allFilePaths[i]);
                    SSH_Helper.Execute($"cd {sshPath};obj2rad -m {Path.GetFileName(allFilePaths[0])} -f {Path.GetFileName(allFilePaths[i])} > {radFilePath}.rad", sb);
                    radFilePaths.Add(radFilePath);
                }
            }
            

            


            DA.SetData("status", sb.ToString());
            DA.SetDataList("rad file paths", radFilePaths);
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
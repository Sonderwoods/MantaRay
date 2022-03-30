using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_PointsToPts : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_PointsToPts class.
        /// </summary>
        public GH_PointsToPts()
          : base("Points To .pts", "Points2pts",
              "Export a list of points and vectors to a pts  file. If no vectors are supplied, we assume vect=Z.\n" +
                "Uploads the pts file to the linux server",
              "Export")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager[pManager.AddVectorParameter("Vectors","Vectors","Vectors. Default is 0,0,1", GH_ParamAccess.list, new Vector3d(0,0,1))].Optional = true;
            pManager[pManager.AddTextParameter("Name", "Name", "Name (will save name.pts)", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("Subfolder", "Subfolder", "Optional. Override the subfolder from the connection component.", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("pts file", "pts file", "pts file", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!DA.Fetch<bool>("Run"))
                return;

            string name = DA.Fetch<string>("Name");

            List<Point3d> pts = DA.FetchList<Point3d>("Points");
            List<Vector3d> vects = DA.FetchList<Vector3d>("Vectors");
            StringBuilder ptsFile = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            

            if (pts.Count == 0)
            {
                throw new Exception("No points");
            }

            if (pts.Count > 1 && vects.Count > 1 && pts.Count != vects.Count)
            {
                throw new Exception("vector count and point count does not match");
            }

            if (vects.Count == 0)
                vects.Add(new Vector3d(0, 0, 1));


            if (vects.Count == 1)
            {
                vects.Capacity = pts.Count;
                while (vects.Count < pts.Count)
                {
                    vects.Add(vects[0]);
                }
            }

            string workingDir;

            string subfolder = DA.Fetch<string>("Subfolder");


            if (string.IsNullOrEmpty(subfolder))
            {
                workingDir = SSH_Helper.WindowsFullpath;
            }
            else
            {
                workingDir = SSH_Helper.WindowsParentPath + "\\" + subfolder;
            }

            workingDir = (workingDir.EndsWith("\\") || workingDir.EndsWith("/")) ? workingDir : workingDir + "\\";

            string ptsFilePath = $"{workingDir}{name}.obj";



            for (int i = 0; i < pts.Count; i++)
            {
                ptsFile.AppendFormat("{0} {1} {2} {3} {4} {5} {6}\r\n", pts[i].X, pts[i].Y, pts[i].Z, vects[i].X, vects[i].Y, vects[i].Z);
            }

            System.IO.File.WriteAllText(ptsFilePath, ptsFile.ToString());


            try
            {
                SSH_Helper.Upload(ptsFilePath, ptsFilePath, sb);

            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException e)
            {
                sb.AppendFormat("Could not upload files - Path not found ({0})! {1}", ptsFilePath, e.Message);
                
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
            get { return new Guid("1FE05399-3EF7-4264-A811-D7AF73769D48"); }
        }
    }
}
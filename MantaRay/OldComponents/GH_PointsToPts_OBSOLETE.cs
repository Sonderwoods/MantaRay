using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using Rhino.Geometry;
using MantaRay.Helpers;
using System.Globalization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Renci.SshNet;

namespace MantaRay.Components
{
    [Obsolete]
    public class GH_PointsToPts_OBSOLETE : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_PointsToPts class.
        /// </summary>
        public GH_PointsToPts_OBSOLETE()
          : base("Points To .pts", "Points2pts",
              "Export a list of points and vectors to a pts  file. If no vectors are supplied, we assume vect=Z.\n" +
                "Uploads the pts file to the linux server",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points", "Points\nIn rhino units. Will be converted to meter!", GH_ParamAccess.list);
            pManager[pManager.AddVectorParameter("Vectors", "Vectors", "Vectors. Default is 0,0,1", GH_ParamAccess.list, new Vector3d(0, 0, 1))].Optional = true;
            pManager[pManager.AddTextParameter("Name", "Name", "Name (will save name.pts)", GH_ParamAccess.item, "points")].Optional = true;
            pManager[pManager.AddTextParameter("Subfolder Override", "Subfolder Override", "Optional. Override the subfolder from the connection component.", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("pts file", "pts file", "pts file in linux paths (they are uploaded already)", GH_ParamAccess.item);
            //pManager.AddTextParameter("pts string", "pts string", "pts string", GH_ParamAccess.item);
            pManager.AddTextParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            TimingHelper th = new TimingHelper("PointsToPts");


            string name = DA.Fetch<string>(this, "Name").ApplyGlobals();


            List<Point3d> pts = DA.FetchList<Point3d>(this, "Points");
            List<Vector3d> vects = DA.FetchList<Vector3d>(this, "Vectors");
            StringBuilder ptsFile = new StringBuilder();
            StringBuilder sb = new StringBuilder();

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());


            string workingDir;

            string subfolderOverride = DA.Fetch<string>(this, "Subfolder Override").ApplyGlobals().Replace('\\', '/').Trim('/');


            if (string.IsNullOrEmpty(subfolderOverride))
            {
                workingDir = sshHelper.WinHome;
            }
            else
            {
                workingDir = sshHelper.WindowsParentPath + "\\" + subfolderOverride;
            }

            workingDir = (workingDir.EndsWith("\\") || workingDir.EndsWith("/")) ? workingDir : workingDir + "\\";


            string ptsFilePath = $"{workingDir}{name}.pts";


            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);


            if (DA.Fetch<bool>(this, "Run"))
            {

                if (sshHelper == null || sshHelper.SftpClient == null || !sshHelper.SftpClient.IsConnected)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No connection");
                    return;
                }

                if (pts.Count == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Points");
                    return;

                }

                if (pts.Count > 1 && vects.Count > 1 && pts.Count != vects.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "vector count and point count does not match");
                    return;
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


                // Create windows directories
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(ptsFilePath)))
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ptsFilePath));




                for (int i = 0; i < pts.Count; i++)
                {
                    ptsFile.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5}\r\n", pts[i].X.ToMeter(), pts[i].Y.ToMeter(), pts[i].Z.ToMeter(), vects[i].X, vects[i].Y, vects[i].Z);
                }


                System.IO.File.WriteAllText(ptsFilePath, ptsFile.ToString());
                //DA.SetData(1, ptsFile.ToString());

                th.Benchmark("write file");

                string linuxPath = string.IsNullOrEmpty(subfolderOverride) ? sshHelper.LinuxHome : sshHelper.LinuxParentPath + "/" + subfolderOverride;

                try
                {

                    //SSH_Helper.Upload(ptsFilePath, linuxPath, sb);
                    sshHelper.Upload(ptsFilePath, linuxPath, sb);


                }
                catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                {
                    sb.AppendFormat("Could not upload files - Path not found ({0})! {1}", ptsFilePath, e.Message);

                }
                th.Benchmark("upload");

                DA.SetData(0, ptsFilePath.ToLinuxPath());
            }
            else
            {
                string p = ptsFilePath.ToLinuxPath();

                th.Benchmark("check if file exists in linux");
                DA.SetData(0, ptsFilePath.ToLinuxPath());
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Reusing points from existing file");


            }


        }

        protected override Bitmap Icon => Resources.Resources.Ra_Pt_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1FE05399-3EF7-4264-A811-D7AF73769D48"); }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;


namespace MantaRay
{
    public class GH_Denoiser : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_Denoiser class.
        /// </summary>
        public GH_Denoiser()
          : base("Denoise image", "Denoiser",
              "Denoise an image using intel open image denoiser library\n\n" +
                "Command line parameters\r\n\r\n" +
                "-v [int] : log verbosity level 0:disabled 1:simple 2:full (default 2)\r\n" +
                "-i [string] : path to input image\r\n-o [string] : path to output image\r\n" +
                "-a [string] : path to input albedo AOV (optional)\r\n" +
                "-n [string] : path to input normal AOV (optional, requires albedo AOV)\r\n" +
                "-hdr [int] : Image is a HDR image. Disabling with will assume the image is in sRGB (default 1 i.e. enabled)\r\n" +
                "-srgb [int] : whether the main input image is encoded with the sRGB (or 2.2 gamma) curve (LDR only) or is linear (default 0 i.e. disabled)\r\n" +
                "-t [int] : number of threads to use (defualt is all)\r\n" +
                "-affinity [int] : Enable affinity. This pins virtual threads to physical cores and can improve performance (default 0 i.e. disabled)\r\n" +
                "-repeat [int] : Execute the denoiser N times. Useful for profiling.\r\n-maxmem [int] : Maximum memory size used by the denoiser in MB\r\n" +
                "-clean_aux [int]: Whether the auxiliary feature (albedo, normal) images are noise-free; recommended for highest quality " +
                "but should not be enabled for noisy auxiliary images to avoid residual noise (default 0 i.e. disabled)\r\n" +
                "-h/--help : Lists command line parameters\r\n" +
                "You need to at least have an input and output for the app to run. If you also have them, you can add an albedo AOV or albedo and normal " +
                "AOVs to improve the denoising. All images should be the same resolutions, " +
                "not meeting this requirement will lead to unexpected results (likely a crash).\r\n\r\n" +
                "For best results provide as many of the AOVs as possible to the denoiser. " +
                "Generally the more information the denoiser has to work with the better. " +
                "The denoiser also prefers images rendered with a box filter or by using FIS.\r\n\r\n" +
                "Please refer to the originial OIDN repository here for more information.",
              "Linkajou", "Misc")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("InputImage", "InputImage", "Path to jpg image", GH_ParamAccess.item);
            pManager.AddTextParameter("params", "params", "for instance can be \"-t 2\" for two cores", GH_ParamAccess.item, "");
            pManager.AddTextParameter("PathToDenoisr", "PathToDenoisr", "PathToDenoisr exe file (get it at ", GH_ParamAccess.item, "C:\\Denoiser_v1.6\\Denoiser.exe");
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager.AddTextParameter("AlbedoImage", "AlbedoImage", "Optional, AlbedoImage to improve denoising results", GH_ParamAccess.item, "");
            pManager.AddTextParameter("NormalsImage", "NormalsImage", "Optional, NormalsImage to improve denoising results", GH_ParamAccess.item, "");
            pManager[3].Optional = true;
            pManager[4].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("OutputImage", "OutputImage", "input", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string imagePath = DA.Fetch<string>(this, 0);
            string @params = DA.Fetch<string>(this, 1);
            string denoisrPath = DA.Fetch<string>(this, 2);
            string albedoImg = DA.Fetch<string>(this, 3);
            string normalsImg = DA.Fetch<string>(this, 4);

            if (!string.IsNullOrEmpty(@params))
            {
                @params += " ";
            }

            if (!File.Exists(imagePath))
                throw new FileNotFoundException(imagePath);

            if (!File.Exists(denoisrPath))
                throw new FileNotFoundException("Could not find the denoiser path. Did you have it installed? Right click the component to add it.", denoisrPath);

            string outImage = $"{Path.GetDirectoryName(imagePath)}\\{Path.GetFileNameWithoutExtension(imagePath)}_denoised{Path.GetExtension(imagePath)}";

            //ExecuteCommand($"{denoisrPath}");
            //ExecuteCommand($"{denoisrPath} -i \"{imagePath}\"");
            if (!string.IsNullOrEmpty(albedoImg))
            {
                albedoImg = $" -a \"{albedoImg}\"";
            }
            if (!string.IsNullOrEmpty(normalsImg))
            {
                normalsImg = $" -n \"{normalsImg}\"";
                if (string.IsNullOrEmpty(albedoImg))
                {
                    throw new Exception("Cant use normalsImg without an albedo");
                }
            }

            ExecuteCommand($"{denoisrPath} {@params}-i \"{imagePath}\"{albedoImg}{normalsImg} -o \"{outImage}\"");

            DA.SetData(0, outImage);




        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("61F245D5-1735-4556-9769-CA5CD970D3C6"); }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {

            Menu_AppendItem(menu, $"Download denoiser", StartWeb);

            base.AppendAdditionalMenuItems(menu);
        }

        public void StartWeb(object sender, EventArgs a)
        {
            Process.Start("https://github.com/DeclanRussell/IntelOIDenoiser/releases/tag/1.6");
        }


        static void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            var process = Process.Start(processInfo);

            //process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            //    Console.WriteLine("output>>" + e.Data);
            //process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                Console.WriteLine("Denoiser: error>>" + e.Data);
            process.BeginErrorReadLine();

            process.WaitForExit();

            Console.WriteLine("ExitCode: {0}", process.ExitCode);
            process.Close();
        }

    }
}
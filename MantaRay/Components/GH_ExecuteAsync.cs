using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MantaRay
{
    public class GH_ExecuteAsync : GH_Template_Async_Extended
    {
        public override Guid ComponentGuid { get => new Guid("22C612B2-2C57-47CE-B2FE-E10621F18933"); }

        protected override System.Drawing.Bitmap Icon => Resources.Resources.Ra_Ra_Icon;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public GH_ExecuteAsync() : base("Execute SSH", "ExecuteSSH", "ExecuteAsync2", "Use me to execute a SSH Command")
        {
            BaseWorker = new SSH_Worker2(this);
        }

        public bool addPrefix = true;
        public bool addSuffix = true;
        public bool suppressWarnings = false;
        //readonly List<int> Pids = new List<int>();

        public List<string> Commands { get; set; } = new List<string>();
        public List<string> Results { get; set; } = new List<string>();
        public List<string> Stderrs { get; set; } = new List<string>();

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SSH Commands", "_SSH commands_", "SSH commands. Each item in list will be executed\n\n" +
                "Do a grafted tree input to run in parallel. However there is no checks if this starts too many CPUs on the host\n" +
                "Use with caution!!", GH_ParamAccess.list);
            pManager[pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.tree, false)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("stdout", "stdout", "stdout", GH_ParamAccess.item);
            pManager.AddTextParameter("stderr", "stderr_", "stderr\nWill output any eventual errors or warnings", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran without any stderr. If you want it to output true even with errors, " +
                "right click on the component and enable suppress warnings.", GH_ParamAccess.tree); //always keep ran as the last parameter
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);


            var m = Menu_AppendItem(menu, "Set Log details...", (s, e) => { SetLogDetails(); }, true);
            m.ToolTipText = "Opens a dialog with settings for local logging";


            Menu_AppendSeparator(menu);


            Menu_AppendItem(menu, "*Add prefix", (s, e) => { addPrefix = !addPrefix; UpdateNickNames(); ExpireSolution(true); }, true, addPrefix)
                .ToolTipText = "Adding a prefix with export settings for SSH. This is on by default. Will recompute!";
            Menu_AppendItem(menu, "*Add suffix", (s, e) => { addSuffix = !addSuffix; UpdateNickNames(); ExpireSolution(true); }, true, addSuffix)
                .ToolTipText = "Adding a suffix to pipe out the PID of the process to allow us to kill it. This is on by default Will recompute!";
            Menu_AppendItem(menu, "*Suppress warnings", (s, e) => { suppressWarnings = !suppressWarnings; UpdateNickNames(); ExpireSolution(true); },
                true, suppressWarnings)
                .ToolTipText = "By default, the ran parameter will only output true if there was no warnings. " +
                "You can however suppress this and make ran_output = run_input\nWill recompute!";


            Menu_AppendSeparator(menu);


            HashSet<string> radProgs = new HashSet<string>();

            foreach (var cmd in Commands.Where(c => c != null))
            {
                foreach (var cmdStart in cmd.Replace("\n", ";").Split(';'))
                {
                    foreach (var prog in ManPageHelper.Instance.AllRadiancePrograms.Keys)
                    {
                        if (cmd.Trim().Contains(prog))
                        {
                            radProgs.Add(prog);

                        }
                    }
                }

            }

            if (radProgs.Count > 0)
            {
                Menu_AppendSeparator(menu);
            }

            foreach (var item in radProgs)
            {
                Menu_AppendItem(menu, $"ManPage: {item}...", (s, e) => { ManPageHelper.Instance.OpenManual(item); });
            }
        }

        //public void LinuxKill()
        //{
        //    foreach (var pid in Pids.Where(p => p > 0))
        //    {
        //        SSH_Helper.Execute($"kill {Pids}", prependPrefix: false);
        //    }

        //}



   
        /// <summary>
        /// Update nick names of input and output to show if warnings are suppressed and prefixes are added.
        /// </summary>
        public void UpdateNickNames()
        {
            Params.Input[0].NickName = (addPrefix ? "_" : "") + "SSH Commands" + (addSuffix ? "_" : "");
            Params.Output[1].NickName = "stderr" + (suppressWarnings ? "" : "_");

        }

        protected override bool PreRunning(IGH_DataAccess DA)
        {
            if (RunCount == 0)
            {
                Commands.Clear();
                Results.Clear();
                Commands.Clear();
            }
            Results.Add(null); // filling the list to match results later. Perhaps this can be done in a better fashion.
            Stderrs.Add(null);
            Commands.Add(null);
            return base.PreRunning(DA);
        }

        public override bool Read(GH_IReader reader)
        {

            reader.TryGetBoolean("addPrefix", ref addPrefix);
            reader.TryGetBoolean("addSuffix", ref addSuffix);
            reader.TryGetBoolean("suppressWarnings", ref suppressWarnings);
            

            string s = String.Empty;

            if (reader.TryGetString("results", ref s))
            {
                Results = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None).ToList();
            }

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {

            writer.SetBoolean("addPrefix", addPrefix);
            writer.SetBoolean("addSuffix", addSuffix);
            writer.SetBoolean("suppressWarnings", suppressWarnings);
            writer.SetString("results", String.Join(">JOIN<", Results));
            writer.SetString("commands", String.Join(">JOIN<", Commands));


            return base.Write(writer);
        }

        public override bool IsPreviewCapable => true;


        public override void ClearCachedData()
        {
            ((SSH_Worker2)BaseWorker).results.Clear();
            ((SSH_Worker2)BaseWorker).stderr.Clear();
            ((SSH_Worker2)BaseWorker).commands.Clear();

            base.ClearCachedData();
            

        }


        public class SSH_Worker2 : WorkerInstance
        {

            public StringBuilder results = new StringBuilder();
            public StringBuilder stderr = new StringBuilder();
            public List<string> commands = new List<string>();

            bool run = false;
            bool ran = false;

            public SSH_Worker2(GH_Component component) : base(component) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                Parent.Hidden = true;

                ((GH_ExecuteAsync)Parent).Commands.AddRange(commands.Distinct().Where(c => !((GH_ExecuteAsync)Parent).Commands.Contains(c)));


                if (!run) { return; }


                if (CancellationToken.IsCancellationRequested) { return; }

                //ReportProgress(Id, 0);


                int pid = -1;
                IAsyncResult asyncResult = null;

                string command = String.Join(";", commands).Replace("\r\n", "\n").AddGlobals();
                Renci.SshNet.SshCommand cmd = null;
                (asyncResult, cmd, pid) = SSH_Helper.ExecuteAsync(command, prependPrefix: ((GH_ExecuteAsync)Parent).addPrefix, ((GH_ExecuteAsync)Parent).addSuffix, HasZeroAreaPolygons);



                // TODO Need to get pid through "beginexecute" instead of "execute" of SSH.
                int p = 0;
                while (true)
                {
                    // Update progress bar as we run
                    if (p++ % 10 == 0 )
                    {
                        ((GH_Template_Async_Extended)Parent).RunTime = ((GH_Template_Async_Extended)Parent).Stopwatch.Elapsed;
                        Parent.Message = "Running for " + ((GH_Template_Async_Extended)Parent).RunTime.ToShortString();
                        Parent.OnDisplayExpired(true);
                    }
                    
                    // If the command finished
                    if (WaitHandle.WaitAll(new[] { asyncResult.AsyncWaitHandle }, 100))
                    {
                        
                        stderr.Append(cmd.Error);
                        results.Append(cmd.Result);
                        bool itsJustAWarning = results.ToString().Contains("warning");
                        ran &= pid > 0 || itsJustAWarning || ((GH_ExecuteAsync)Parent).suppressWarnings;

                        break;
                    }
                    

                    // Cancelled
                    if (CancellationToken.IsCancellationRequested)
                    {
                        cmd.CancelAsync();
                        ran = false;
                        return;
                    }

                }

                //ran = false;



                //if (((GH_ExecuteAsync)Parent).savedResults.Any(s => !String.IsNullOrEmpty(s.Stdout.ToString())))
                //{
                //    results = ((GH_ExecuteAsync)Parent).savedResults;
                //}


                Done();

            }


            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                //if (CancellationToken.IsCancellationRequested) return;
                commands = DA.FetchList<string>(0);

                List<GH_Boolean> _runs = DA.FetchTree<GH_Boolean>(1).FlattenData();

                run = _runs.Count > 0 && _runs.All(g => g?.Value == true);

            }

            bool HasZeroAreaPolygons(string errors)
            {
                return !errors.StartsWith("oconv: warning - zero area");
            }

            public override void SetData(IGH_DataAccess DA)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    //Parent.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Cancelled");
                    //((GH_Template_Async_Extended)Parent).PhaseForColors = AestheticPhase.Cancelled;

                    //((GH_ColorAttributes_Async)Parent.Attributes).ColorSelected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.DarkRed);
                    //((GH_ColorAttributes_Async)Parent.Attributes).ColorUnselected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.DarkOrchid);


                    //Parent.Message = "Cancelled X";
                    return;
                }


                ((GH_ExecuteAsync)Parent).Results[Id] = results.ToString();
                ((GH_ExecuteAsync)Parent).Stderrs[Id] = stderr.ToString();

                DA.SetData(0, results.ToString());

                DA.SetData(1, stderr.ToString());

                var runOut = new GH_Structure<GH_Boolean>();
                runOut.Append(new GH_Boolean(ran), new GH_Path(0));
                Parent.Params.Output[2].ClearData();
                DA.SetDataTree(2, runOut);


                Parent.AddRuntimeMessage(stderr.ToString().Contains("error") ? GH_RuntimeMessageLevel.Error : GH_RuntimeMessageLevel.Warning, stderr.ToString());


            }

            public override WorkerInstance Duplicate() => new SSH_Worker2(Parent);


        }



    }


}

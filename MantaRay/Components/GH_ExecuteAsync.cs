using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Plugin;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using MantaRay.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace MantaRay
{

    //TODO: Get outputs while running -> https://www.linuxfixes.com/2022/04/solved-sshnet-real-time-command-output.html
    public class GH_ExecuteAsync : GH_Template_Async_Extended, IHasDoubleClick
    {
        public override Guid ComponentGuid { get => new Guid("22C612B2-2B57-47CE-A2FE-E10221F18933"); }

        const string JOIN = "\n_JOIN_\n";

        protected override System.Drawing.Bitmap Icon => Resources.Resources.Ra_Ra_Icon;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public GH_ExecuteAsync() : base("Execute SSH", "ExecuteSSH", "Use me to execute a SSH Command", "1 SSH")
        {
            BaseWorker = new SSH_Worker(this);
            RunTime = new TimeSpan(0, 0, 0, 0, (int)LastRun.TotalMilliseconds);
        }

        public bool addPrefix = true;
        public bool addSuffix = false;
        public bool suppressWarnings = false;
        public TimeSpan LastRun = default;
        //readonly List<int> Pids = new List<int>();

        public List<string> Commands { get; set; } = new List<string>();
        public List<string> Results { get; set; } = new List<string>();
        public List<string> Stderrs { get; set; } = new List<string>();

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SSH Commands", "_SSH commands_", "SSH commands. Each item in list will be executed\n\n" +
                "Do a grafted tree input to run in parallel. However there is no checks if this starts too many CPUs on the host\n" +
                "Use with caution!!", GH_ParamAccess.list);
            pManager[pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.tree, false)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("stdout", "stdout", "stdout", GH_ParamAccess.list);
            pManager.AddTextParameter("stderr", "stderr_", "stderr\nWill output any eventual errors or warnings", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran without any stderr. If you want it to output true even with errors, " +
                "right click on the component and enable suppress warnings.", GH_ParamAccess.tree); //always keep ran as the last parameter
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);


            Menu_AppendSeparator(menu);

            var rr = Menu_AppendItem(menu, "Rerun", (s, e) => { ExpireSolution(true); }, true);
            rr.ToolTipText = "Expire the component";


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

            List<string> commands = new List<string>();

            if (Params.Input[0].Phase == GH_SolutionPhase.Blank)
            {
                this.Params.Input[0].CollectData();

            }
            foreach (IGH_Goo data in this.Params.Input[0].VolatileData.AllData(false))
            {
                switch (data)
                {
                    case GH_String b:
                        if (b.IsValid) { commands.Add(b.Value); }
                        break;
                    case OverridableText ot:
                        if (ot.IsValid) { commands.Add(ot.Value); }
                        break;

                    default:

                        break;
                }

            }

            this.Params.Input[0].ClearData();



            try
            {
                foreach (var cmd in commands.Where(c => c != null))
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

                        if (
                            cmd.Trim().Contains("rtpict") ||
                            cmd.Trim().Contains("rtcontrib") ||
                            cmd.Trim().Contains("rcontrib") ||
                            cmd.Trim().Contains("rpiece") ||
                            cmd.Trim().Contains("rfluxmtx"))
                        {
                            radProgs.Add("rpict");
                        }
                    }

                }
            }
            catch (Exception)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to do the manpage section");
                //throw new Exception("Failed the contextmenu.." +  e.Message);
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

            base.PreRunning(DA);

            this.Hidden = !RunInput;

            if (RunCount == 1 && RunInput)
            {
                Commands.Clear();
                Results.Clear();
                Stderrs.Clear();

            }
            if (RunInput)
            {
                Results.Add(null); // filling the list to match results later. Perhaps this can be done in a better fashion.
                Stderrs.Add(null);
                Commands.Add(null);
            }

            return RunInput;
        }

        protected override void PostRunning(IGH_DataAccess DA)
        {
            base.PostRunning(DA);

            // In case it was set to false and we want to still output saved results
            if (!RunInput)
            {
                if (RunCount == 1)
                {
                    Params.Output[0].ClearData();
                    Params.Output[1].ClearData();


                    SetOneBoolOutput(this, DA, 2, false);
                }

                if (Results.Count > RunCount - 1)
                    DA.SetDataList(0, Results[RunCount - 1] != null ? Results[RunCount - 1].Split(new[] { JOIN }, StringSplitOptions.None).Select(v => v.Trim('\n', '\r')).Where(r => r != JOIN && r.Trim() != "_JOIN_").ToArray() : new string[0]);
                else
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No cached data, please rerun.");

                if (Stderrs.Count > RunCount - 1)
                    DA.SetDataList(1, Stderrs[RunCount - 1] != null ? Stderrs[RunCount - 1].Split(new[] { JOIN }, StringSplitOptions.None).Select(v => v.Trim('\n', '\r')).ToArray() : new string[0]);
                else
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No cached data, please rerun.");


                


                Message = LastRun.TotalMilliseconds > 0 ? $"Cached  (last was {LastRun.ToShortString()})" : "Clean";

                if (LastRun.TotalMilliseconds > 0)
                {
                    PhaseForColors = AestheticPhase.Reusing;
                    //((GH_ColorAttributes_Async)m_attributes).ColorSelected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.FromArgb(76, 128, 122));
                    //((GH_ColorAttributes_Async)m_attributes).ColorUnselected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.FromArgb(95, 115, 113));
                }

            }
            else
            {
                if (Params.Output[this.Params.Output.Count - 1].VolatileDataCount == 0)
                {
                    DA.SetData(this.Params.Output.Count - 1, false);

                }

                Message = LastRun.TotalMilliseconds > 0 ? $"Ran in {RunTime.ToShortString()} (last was {LastRun.ToShortString()})" : $"Ran in {RunTime.ToShortString()}";
                LastRun = RunTime;
                this.Hidden = false;
            }

        }

        public override bool Read(GH_IReader reader)
        {

            reader.TryGetBoolean("addPrefix", ref addPrefix);
            //reader.TryGetBoolean("addSuffix", ref addSuffix);
            reader.TryGetBoolean("suppressWarnings", ref suppressWarnings);
            double lastRun = 0;
            if (reader.TryGetDouble("lastRunTime", ref lastRun))
            {
                LastRun = new TimeSpan(0, 0, 0, 0, (int)lastRun);

            }
            Results.Clear();
            Commands.Clear();
            Stderrs.Clear();

            string s = String.Empty;
            for (int i = 0; i < int.MaxValue; i++)
            {
                if (reader.ItemExists("results", i))
                {
                    if (reader.TryGetString("results", i, ref s))
                        Results.Add(s);
                }
                else
                    break;
            }

            for (int i = 0; i < int.MaxValue; i++)
            {
                if (reader.ItemExists("stderr", i))
                {
                    if (reader.TryGetString("stderr", i, ref s))
                        Stderrs.Add(s);
                }
                else
                    break;
            }

            for (int i = 0; i < int.MaxValue; i++)
            {
                if (reader.ItemExists("commands", i))
                {
                    if (reader.TryGetString("commands", i, ref s))
                        Commands.Add(s);
                }
                else
                    break;
            }



            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            try
            {
                writer.SetDouble("lastRunTime", LastRun.TotalMilliseconds);
                writer.SetBoolean("addPrefix", addPrefix);
                //writer.SetBoolean("addSuffix", addSuffix);
                writer.SetBoolean("suppressWarnings", suppressWarnings);

                for (int i = 0; i < Results.Count; i++)
                {
                    writer.SetString("results", i, Results[i]);
                }

                for (int i = 0; i < Stderrs.Count; i++)
                {
                    writer.SetString("stderr", i, Stderrs[i]);
                }

                for (int i = 0; i < Commands.Count; i++)
                {
                    writer.SetString("commands", i, Commands[i]);
                }
            }
            catch (Exception)
            {
                throw new Exception("WRITE errors");
            }


            return base.Write(writer);
        }

        public override bool IsPreviewCapable => true;


        public override void ClearCachedData()
        {

            Results.Clear();
            Stderrs.Clear();
            Commands.Clear();
            LastRun = new TimeSpan();

            base.ClearCachedData();


        }

        public GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            SetLogDetails();
            return GH_ObjectResponse.Handled;
        }


        public class SSH_Worker : WorkerInstance
        {

            public string results = String.Empty;
            public string stderr = String.Empty;
            public List<string> commands = new List<string>();

            bool run = false;
            bool ran = false;

            public SSH_Worker(GH_Component component) : base(component) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {

                Parent.Hidden = true;

                ((GH_ExecuteAsync)Parent).Commands.Add(String.Join(JOIN, commands.Distinct().Where(c => !((GH_ExecuteAsync)Parent).Commands.Contains(c))));

                if (CancellationToken.IsCancellationRequested) { return; }

                //ReportProgress(Id, 0);


                int pid = -1;
                IAsyncResult asyncResult = null;

                if (run)
                {

                    string command = String.Join(";echo _JOIN_;", commands).Replace("\r\n", "\n").ApplyGlobals();

                    


                    Renci.SshNet.SshCommand cmd = null;
                    (asyncResult, cmd, pid) = SSH_Helper.ExecuteAsync(command, prependPrefix: ((GH_ExecuteAsync)Parent).addPrefix, ((GH_ExecuteAsync)Parent).addSuffix, HasZeroAreaPolygons);


                    // TODO Need to get pid through "beginexecute" instead of "execute" of SSH.
                    int counter = 0;
                    int intervalForRefreshing = 100; //ms in each loop below.
                    int waitingForNewConnection = 5; // max iterations after checkconnection fails before throwing cancellation (this will give 1000ms to reconnect before changing context)
                    int dotCounter = 0;
                    string[] dots = new[] { "", ".", "..", "..." };
                    while (true && asyncResult != null)
                    {
                        // Update progress bar as we run
                        if (counter++ % 5 == 0)
                        {
                            ((GH_Template_Async_Extended)Parent).RunTime = ((GH_Template_Async_Extended)Parent).Stopwatch.Elapsed;
                            if (((GH_Template_Async_Extended)Parent).RunTime.TotalSeconds >= 60)
                            {
                                Parent.Message = "Running for " + ((GH_Template_Async_Extended)Parent).RunTime.ToShortString() + dots[dotCounter] + " (Last: " + ((GH_ExecuteAsync)Parent).LastRun.ToShortString() + ")";
                                if (dotCounter++ >= dots.Length - 1)
                                    dotCounter = 0;
                            }
                            else
                            {
                                Parent.Message = "Running for " + ((GH_Template_Async_Extended)Parent).RunTime.ToShortString() + " (Last: " + ((GH_ExecuteAsync)Parent).LastRun.ToShortString() + ")";

                            }


                            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                            {
                                Parent.OnDisplayExpired(true);
                            });
                        }



                        // If the command finished
                        if (WaitHandle.WaitAll(new[] { asyncResult.AsyncWaitHandle }, intervalForRefreshing))
                        {
                            // TODO: eventually use SshCommand.EndExecute
                            stderr = string.IsNullOrEmpty(cmd.Error) ? null : cmd.Error;
                            results = cmd.Result;
                            bool itsJustAWarning = stderr?.ToString().Contains("warning") ?? false;
                            ran = stderr == null || itsJustAWarning || ((GH_ExecuteAsync)Parent).suppressWarnings;

                            break;
                        }

                        if (SSH_Helper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected && waitingForNewConnection-- <= 0)
                        {
                            ((GH_ExecuteAsync)Parent).RequestCancellation();
                            //ran = false;
                            //break;

                            // TODO: Make an error that we lost the connection?

                        }


                        // Cancelled
                        if (CancellationToken.IsCancellationRequested)
                        {
                            cmd.CancelAsync();
                            ran = false;
                            break;
                            //return;
                        }

                    }
                }


                Done();

            }


            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                //if (CancellationToken.IsCancellationRequested) return;

                //By calling ToString on the overridable text we make sure that the overrides are done.
                commands = DA.FetchList<OverridableText>(0).Select(s => s.Value).ToList();

                foreach (var command in commands)
                {
                    if(command.EndsWith("Execute component"))
                    {
                        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There's a risk that you are using a manipulated text output from ApplyOverrides.\n" +
                            "You should connect the component directly to this one and not do any editing between them");
                        break;
                    }
                }


                StringBuilder sb = new StringBuilder();
                GH_Structure<IGH_Goo> data = ((GH_ExecuteAsync)Parent).Params.Input[0].VolatileData as GH_Structure<IGH_Goo>;


                for (int i = 0; i < data.Branches.Count; i++)
                {
                    List<IGH_Goo> branch = data.Branches[i];
                    foreach (IGH_Goo item in branch.Where(it => it != null))
                    {
                        if (typeof(OverridableText).IsAssignableFrom(item.GetType()))
                        {
                            sb.AppendFormat("{0}\n", ((OverridableText)item).Value);
                        }

                    }
                    if (i < data.Branches.Count - 1)
                        sb.Append("\n\n- - -\n\n");

                }
                ((GH_ExecuteAsync)Parent).LogDescriptionDynamic = sb.ToString();

                List<GH_Boolean> _runs = DA.FetchTree<GH_Boolean>(1).FlattenData();

                run = _runs.Count > 0 && _runs.All(g => g?.Value == true);

            }



            bool HasZeroAreaPolygons(string errors)
            {
                return !errors.StartsWith("oconv: warning - zero area");
            }

            /// <summary>
            /// This is a successful run! We hope.
            /// </summary>
            /// <param name="DA"></param>
            public override void SetData(IGH_DataAccess DA)
            {
                foreach (var command in ((GH_ExecuteAsync)Parent).Commands)
                {
                    if (command != null && command.EndsWith("Execute component"))
                    {
                        Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There's a risk that you are using a manipulated text output from ApplyOverrides.\n" +
                            "You should connect the component directly to this one and not do any editing between them");
                        break;
                    }
                }

                if (CancellationToken.IsCancellationRequested)
                {

                    return;
                }

                // Saving to persistant data in component
                if (((GH_ExecuteAsync)Parent).Results[Id] == null && results != null)
                    ((GH_ExecuteAsync)Parent).Results[Id] = results;
                else
                    results = ((GH_ExecuteAsync)Parent).Results[Id];

                if (((GH_ExecuteAsync)Parent).Stderrs[Id] == null && stderr != null)
                    ((GH_ExecuteAsync)Parent).Stderrs[Id] = stderr;
                else
                    stderr = ((GH_ExecuteAsync)Parent).Stderrs[Id];




                DA.SetDataList(0, results != null && !String.Equals(results, JOIN) ? results.Split(new[] { JOIN }, StringSplitOptions.None).Select(b => b.Trim('\n', '\r')).Where(r => r != JOIN && r.Trim() != "_JOIN_") : new string[] { null });

                DA.SetDataList(1, stderr != null ? stderr.Split(new[] { JOIN }, StringSplitOptions.None).Select(b => b.Trim('\n', '\r')) : new string[] { null });

                //Set only ONE bool output in "RAN"
                SetOneBoolOutput(Parent, DA, 2, ran);


                if (stderr != null)
                {
                    Parent.AddRuntimeMessage(stderr.ToString().Contains("error") ? GH_RuntimeMessageLevel.Error : GH_RuntimeMessageLevel.Warning, stderr.ToString());

                }


            }

            public override WorkerInstance Duplicate() => new SSH_Worker(Parent);


        }



    }


}

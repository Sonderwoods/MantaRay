using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Documentation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public class GH_ExecuteAsync : GH_TemplateAsync
    {
        /// <summary>
        /// Initializes a new instance of the GH_Execute class.
        /// </summary>
        public GH_ExecuteAsync()
          : base("Execute SSH Async", "Execute SSH Async",
              "Use me to execute a SSH Command",
              "1 SSH")
        {

            BaseWorker = new SSH_Worker(this);
            Hidden = true;

        }

        public string[] commands = new string[0];
        public bool addPrefix = true;
        public bool addSuffix = true;
        public bool suppressWarnings = false;

        public RunInfo[] savedResults = new RunInfo[0];

        public bool FirstRun { get; set; } = true;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SSH Commands", "_SSH commands_", "SSH commands. Each item in list will be executed\n\n" +
                "Do a grafted tree input to run in parallel. However there is no checks if this starts too many CPUs on the host\n" +
                "Use with caution!!", GH_ParamAccess.tree);
            pManager[pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.tree, false)].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("stdout", "stdout", "stdout", GH_ParamAccess.list);
            pManager.AddTextParameter("stderr", "stderr_", "stderr\nWill output any eventual errors or warnings", GH_ParamAccess.list);
            pManager.AddTextParameter("log", "log", "log", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Pid", "Pid", "Linux process id. Can be used to kill the task if it takes too long. " +
                "Simply write in a bash prompt: kill <id>", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Ran", "Ran", "Ran without any stderr. If you want it to output true even with errors, " +
                "right click on the component and enable suppress warnings.", GH_ParamAccess.tree); //always keep ran as the last parameter
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            if(PhaseForColors == AestheticPhase.Running)
            {
                Menu_AppendItem(menu, "Cancel and kill linux", (s, e) => { LinuxKill(); RequestCancellation(); })
                .ToolTipText = "Currently not working... >_< Instead open bash and kill the pid with kill <id>";
            }
            
            Menu_AppendItem(menu, "Add prefix", (s, e) => { addPrefix = !addPrefix; UpdateNickNames();  ExpireSolution(true); }, true, addPrefix)
                .ToolTipText = "Adding a prefix with export settings for SSH. This is on by default";
            Menu_AppendItem(menu, "Add suffix", (s, e) => { addSuffix = !addSuffix; UpdateNickNames();  ExpireSolution(true); }, true, addSuffix)
                .ToolTipText = "Adding a suffix to pipe out the PID of the process to allow us to kill it. This is on by default";
            Menu_AppendItem(menu, "Suppress warnings", (s, e) => { suppressWarnings = !suppressWarnings; UpdateNickNames(); ExpireSolution(true); },
                true, suppressWarnings)
                .ToolTipText = "By default, the ran parameter will only output true if there was no warnings. " +
                "You can however suppress this and make ran_output = run_input";
            Menu_AppendItem(menu, "Set Log details", (s, e) => { SetLogDetails(); }, true)
                .ToolTipText = "Opens a dialog with settings for local logging";
            Menu_AppendItem(menu, "Clear cached stdout", (s, e) => { savedResults = new RunInfo[0]; RunTime = 0; ExpireSolution(true); }, !RunInput)
                .ToolTipText = "Removes the data saved in the component.";
        }

        

        public void UpdateNickNames()
        {
            Params.Input[0].NickName = (addPrefix ? "_" : "") + "SSH Commands" + (addSuffix ? "_" : "");
            Params.Output[1].NickName = "stderr" + (suppressWarnings ? "" : "_");
            
        }

        private void SetLogDetails()
        {

            Font inputFont = new Font("Arial", 10.0f,
                        FontStyle.Regular);

            Font font = new Font("Arial", 10.0f,
                        FontStyle.Bold);

            Form prompt = new Form()
            {
                Width = 820,
                Height = 660,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Set Logging Details",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(255, 185, 185, 185),
                ForeColor = Color.FromArgb(255, 30, 30, 30),
                Font = font

            };
            CheckBox checkBox = new CheckBox()
            {
                Text = "Set log entry details for this component",
                Left = 50,
                Top = 50,
                Width = 500,
                Height = 40,
                Checked = LogSave

            };
            //Label label = new Label() { Left = 50, Top = 50, Width = 700, Height = 40, Text = $"This will set log entry details for this component" };

            Label nameLabel = new Label() { Left = 50, Top = 100, Width = 700, Height = 25, Text = $"Name/Header" };
            TextBox nameTextBox = new TextBox()
            {
                Left = 50,
                Top = 125,
                Width = 700,
                Height = 25,
                Text = LogName,
                ForeColor = Color.FromArgb(42, 48, 40),
                Font = inputFont,
                BackColor = Color.FromArgb(148, 180, 140),
                Margin = new Padding(2),

            };


            CheckBox useDescriptionCheckBox = new CheckBox()
            {
                Left = 50,
                Top = 160,
                Width = 700,
                Height = 25,
                Checked = LogUseFixedDescription,
                Text = $"Use fixed description?"
            };
            TextBox descriptionTextBox = new TextBox()
            {
                Left = 50,
                Top = 185,
                Width = 700,
                Height = 340,
                Text = LogDescriptionDynamic,
                Multiline = true,
                AcceptsReturn = true,
                ForeColor = Color.FromArgb(42, 48, 40),
                Font = inputFont,
                BackColor = Color.FromArgb(148, 180, 140),
                Margin = new Padding(2),
                Enabled = LogUseFixedDescription,
                ScrollBars = ScrollBars.Vertical,

            };

            useDescriptionCheckBox.CheckedChanged += UseDescriptionCheckBox_CheckedChanged;

            Button okButton = new Button() { Text = "Ok", Left = 50, Width = 100, Top = 545, Height = 40, DialogResult = DialogResult.OK };
            Button cancelButton = new Button() { Text = "Cancel", Left = 170, Width = 100, Top = 545, Height = 40, DialogResult = DialogResult.Cancel };

            prompt.Controls.AddRange(new Control[] { checkBox, nameLabel, nameTextBox, useDescriptionCheckBox, descriptionTextBox, okButton, cancelButton });

            prompt.AcceptButton = okButton;

            DialogResult result = prompt.ShowDialog();

            if (result == DialogResult.OK)
            {
                LogName = nameTextBox.Text;
                LogDescriptionDynamic = descriptionTextBox.Text;
                LogSave = checkBox.Checked;
                LogUseFixedDescription = useDescriptionCheckBox.Checked;

            }

            void UseDescriptionCheckBox_CheckedChanged(object sender, EventArgs e)
            {
                if (useDescriptionCheckBox.Checked)
                {
                    descriptionTextBox.Enabled = true;
                    if (!String.IsNullOrEmpty(LogDescriptionStatic))
                    {
                        descriptionTextBox.Text = LogDescriptionStatic;
                    }

                }
                else
                {
                    descriptionTextBox.Enabled = false;
                    LogDescriptionStatic = descriptionTextBox.Text;
                    descriptionTextBox.Text = LogDescriptionDynamic;
                }
            }

        }



        protected override void PerformIfInactive(IGH_DataAccess DA)
        {
            if (FirstRun)
            {
                UpdateNickNames();
            }

            DA.SetData("Ran", false);

            this.Hidden = true;

            if (RunInput == false && savedResults.Any(s => !String.IsNullOrEmpty(s.Stdout.ToString())))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old existing stdout\nThis can be convenient for opening old workflows and not running everything again.");
                DA.SetDataList(0, savedResults.Select(r => r.Stdout.ToString()));
                Message = "Reusing results";
                OnDisplayExpired(true);
            }

            base.PerformIfInactive(DA);
        }

        public void LinuxKill()
        {
            if (Pids > 0)
                SSH_Helper.Execute($"kill {Pids}", prependPrefix: false);
        }

        public override void RequestCancellation()
        {
            PhaseForColors = AestheticPhase.NotRunning;
            this.Hidden = true;
            base.RequestCancellation();
        }




        public class RunInfo
        {
            public List<string> Commands { get; set; } = new List<string>();
            public bool Ran { get; set; } = false;
            public int Pid { get; set; } = -1;
            public StringBuilder Stdout { get; set; } = new StringBuilder();
            public StringBuilder Stderr { get; set; } = new StringBuilder();
            public StringBuilder Log { get; set; } = new StringBuilder();
            public bool Success { get; set; } = false;


        }



        public class SSH_Worker : WorkerInstance
        {

            // Define all the parameters for the component here

            public GH_Structure<GH_String> commands = new GH_Structure<GH_String>();

            public bool run = false;

            public bool ran = false;

            public RunInfo[] results = new RunInfo[0];




            public SSH_Worker(GH_Component component) : base(component) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {

                
                bool HasZeroAreaPolygons(string errors)
                {
                    return !errors.StartsWith("oconv: warning - zero area");
                }

                if (CancellationToken.IsCancellationRequested) { return; }


                if (run)
                {

                    ReportProgress(Id, 0);

                    object myLock = new object();
                    results = new RunInfo[commands.Branches.Count];
                    var cmds = new string[commands.Branches.Count];


                    //TODO: Really, really bad to use parallel for locally to execute something on SSH in parallel. Superbug imo.
                    //Good thing is that noone uses it. Right?
                    Parallel.For(0, commands.Branches.Count, i =>
                    {

                        RunInfo result = new RunInfo();

                        int pid = -1;

                        string command = String.Join(";", commands.Branches[i].Select(c => c.Value)).AddGlobals().Replace("\r\n", "\n");

                        pid = SSH_Helper.Execute(command, result.Log, result.Stdout, result.Stderr, prependPrefix: ((GH_ExecuteAsync)Parent).addPrefix, ((GH_ExecuteAsync)Parent).addSuffix, HasZeroAreaPolygons);

                        // TODO Need to get pid through "beginexecute" instead of "execute" of SSH.

                        bool itsJustAWarning = result.Stderr.ToString().Contains("warning");

                        result.Success = pid > 0 || itsJustAWarning || ((GH_ExecuteAsync)Parent).suppressWarnings;

                        if (result.Success)
                        {
                            result.Pid = pid;
                            result.Ran = true;

                        }

                        lock (myLock)
                        {
                            results[i] = result;
                            cmds[i] = command;
                        }

                    });


                    ((GH_ExecuteAsync)Parent).LogDescriptionDynamic = string.Join("\n", cmds);
                    ((GH_ExecuteAsync)Parent).savedResults = results;


                }
                else //run==false
                {
                    ran = false;

                    Parent.Hidden = true;

                    if (((GH_ExecuteAsync)Parent).savedResults.Any(s => !String.IsNullOrEmpty(s.Stdout.ToString())))
                    {
                        results = ((GH_ExecuteAsync)Parent).savedResults;
                    }

                }

                ran = run && results.All(r => r.Ran);

                Done();
            }


            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                //if (CancellationToken.IsCancellationRequested) return;
                commands = DA.FetchTree<GH_String>(0);

                List<GH_Boolean> _runs = DA.FetchTree<GH_Boolean>(1).FlattenData();

                run = _runs.Count > 0 && _runs.All(g => g?.Value == true);

                //SkipRun = ((GH_ExecuteAsync)Parent).FirstRun || !run;  // was AND
                SkipRun = !run;

                ((GH_ExecuteAsync)Parent).FirstRun = false;

            }

            public override void SetData(IGH_DataAccess DA)
            {
                //if (CancellationToken.IsCancellationRequested) return;

                if (run)
                {
                    Parent.Hidden = false;
                }

                //if (run == false && ((GH_ExecuteAsync)Parent).savedResults.Any(s => !String.IsNullOrEmpty(s.Stdout.ToString())))
                //{
                //    Parent.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Using an old existing stdout\nThis can be convenient for opening old workflows and not running everything again.");
                //}
                if (results == null || results.Length == 0 || results.Any(r => r == null))
                    return;

                DA.SetDataList(0, results.Select(r => r.Stdout.ToString()));

                DA.SetDataList(1, results.Select(r => r.Stderr.ToString()));

                DA.SetDataList(2, results.Select(r => r.Log.ToString()));

                DA.SetDataList(3, results.Select(r => r.Pid));

                var runOut = new GH_Structure<GH_Boolean>();
                runOut.Append(new GH_Boolean(ran), new GH_Path(0));
                DA.SetDataTree(4, runOut);


                foreach (string msg in results?.Where(r => !r.Success).Select(r => r.Stderr.ToString()))
                {
                    Parent.AddRuntimeMessage(msg.ToLower().Contains("error") ? GH_RuntimeMessageLevel.Error : GH_RuntimeMessageLevel.Warning, msg);
                }

            }

            public override WorkerInstance Duplicate() => new SSH_Worker(Parent);
        }

        public override bool Write(GH_IWriter writer)
        {

            //writer.SetString("stdouts", String.Join(">JOIN<", ((SSH_Worker)BaseWorker).results.Select(r => r.Stdout)));
            writer.SetString("stdouts", String.Join(">JOIN<", savedResults.Select(r => r.Stdout)));
            writer.SetString("description", LogDescriptionDynamic);
            writer.SetString("staticDescription", LogDescriptionStatic);
            writer.SetString("name", LogName);
            writer.SetBoolean("addPrefix", addPrefix);
            writer.SetBoolean("addSuffix", addSuffix);
            writer.SetBoolean("suppressWarnings", suppressWarnings);
            writer.SetBoolean("fireLogs", LogSave);




            return base.Write(writer);
        }



        public override bool Read(GH_IReader reader)
        {
            string s = String.Empty;
            bool logSave = false;

            if (reader.TryGetString("stdouts", ref s))
            {
                string[] splitString = s.Split(new[] { ">JOIN<" }, StringSplitOptions.None);

                savedResults = new RunInfo[splitString.Length];

                for (int i = 0; i < splitString.Length; i++)
                {
                    savedResults[i] = new RunInfo() { Stdout = new StringBuilder(splitString[i]) };
                }
            }

            if (reader.TryGetString("description", ref s))
            {
                LogDescriptionDynamic = s;
            }
            if (reader.TryGetString("staticDescription", ref s))
            {
                LogDescriptionStatic = s;
            }
            if (reader.TryGetString("name", ref s))
            {
                LogName = s;
            }

            reader.TryGetBoolean("addPrefix", ref addPrefix);
            reader.TryGetBoolean("addSuffix", ref addSuffix);
            reader.TryGetBoolean("suppressWarnings", ref suppressWarnings);

            if (reader.TryGetBoolean("fireLogs", ref logSave))
            {
                LogSave = logSave;
            }

            return base.Read(reader);
        }

        public override bool IsPreviewCapable => true;
        protected override Bitmap Icon => Resources.Resources.Ra_Ra_Icon;
        public override Guid ComponentGuid => new Guid("257C7A8C-330E-43F5-AC62-19F517A3F528");

    }
}
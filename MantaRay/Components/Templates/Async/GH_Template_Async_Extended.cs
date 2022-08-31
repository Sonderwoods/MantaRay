using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace MantaRay
{
    public abstract class GH_Template_Async_Extended : GH_Template_Async
    {
        public enum AestheticPhase
        {
            Running,
            Reusing,
            NotRunning,
            Cancelled
        }

        Guid latestLogGuid = new Guid();

        LogHelper logHelper = null;



        public bool RunInput { get; set; } = false;

        public Stopwatch Stopwatch { get; set; } = new Stopwatch();
        public TimeSpan RunTime { get; set; }

        public virtual bool HasLogAbilities() => true;

        public string LogDescriptionDynamic { get; set; }
        public string LogDescriptionStatic { get; set; }
        public string LogName { get; set; }
        public bool LogSave { get; set; }
        public bool LogUseFixedDescription { get; set; }

        public AestheticPhase PhaseForColors { get; set; } = AestheticPhase.NotRunning;


        protected GH_Template_Async_Extended(string name, string nickname, string description, string subCategory) : base(name, nickname, description, subCategory)
        {
            logHelper = LogHelper.Default;
            DisplayProgressTimer = new Timer(333) { AutoReset = false };
            DisplayProgressTimer.Elapsed += DisplayProgress;

            ReportProgress = (id, value) =>
            {
                ProgressReports[id] = value;
                if (!DisplayProgressTimer.Enabled)
                {
                    DisplayProgressTimer.Start();
                }
            };

            Done = DoDone;

            ProgressReports = new ConcurrentDictionary<string, double>();

            Workers = new List<WorkerInstance>();
            CancellationSources = new List<CancellationTokenSource>();
            Tasks = new List<Task>();

        }

        /// <summary>
        /// Called in the <see cref="DoDone"/> method of the <see cref="GH_Template_Async_Extended"/>
        /// Remember to call base as it will set Log and RunTimes
        /// </summary>
        protected virtual void AfterDone()
        {
            if (HasLogAbilities() && LogSave && RunCount == 1)
            {

                

                //logHelper.Add(LogName, $"Done in {Stopwatch.Elapsed.ToReadableString()}", InstanceGuid);
                logHelper.FinishTask(latestLogGuid);
            }
            if (Tasks.Count == 0)
            {
                RunTime = Stopwatch.Elapsed;

            }

        }

        /// <summary>
        /// DoDone is the Done() Delegate fed into the <see cref="GH_Template_Async_Extended"/>.
        /// </summary>
        protected virtual void DoDone()
        {

            Interlocked.Increment(ref State);
            if (State == Workers.Count && SetData == 0)
            {
                Interlocked.Exchange(ref SetData, 1);

                // We need to reverse the workers list to set the outputs in the same order as the inputs. 
                Workers.Reverse();

                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    ExpireSolution(true);
                });

            }



            AfterDone();



        }

        public override void ClearCachedData()
        {
            base.ClearCachedData();
            RunTime = default;
            // Here is the place we would clear saved variables
        }

        public override void RequestCancellation()
        {
            PhaseForColors = AestheticPhase.Cancelled;
            ((GH_ColorAttributes_Async)m_attributes).ColorSelected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.Firebrick);
            ((GH_ColorAttributes_Async)m_attributes).ColorUnselected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.DarkRed);
            logHelper.FinishTask(latestLogGuid, "Cancelled");
            base.RequestCancellation();
        }

        public virtual void SetLogDetails()
        {
            if (!HasLogAbilities())
                return;

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
                Text = LogUseFixedDescription ? LogDescriptionStatic : LogDescriptionDynamic,
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
                if (useDescriptionCheckBox.Checked)
                {
                    LogDescriptionStatic = descriptionTextBox.Text;

                }
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

        public static void SetOneBoolOutput(GH_Component component, IGH_DataAccess DA, int param, bool result)
        {
            //Set only ONE bool output in "RAN"
            var runOut = new GH_Structure<GH_Boolean>();
            runOut.Append(new GH_Boolean(result), new GH_Path(0));
            component.Params.Output[param].ClearData();
            DA.SetDataTree(param, runOut);
        }

        protected override void ExpireDownStreamObjects()
        {
            // Prevents the flash of null data until the new solution is ready
            if (!firstRun && (SetData == 1 || (!RunInput && RunCount == 1 || RunCount == -1)))
            {
                base.ForceExpireDownStreamObjects();

            }
            firstRun = false;
        }

        protected override void PostRunning(IGH_DataAccess DA)
        {

            //Message = "Done";
            //RunTime = Stopwatch.Elapsed;


            //if (Workers.Any(w => w.CancellationToken.IsCancellationRequested))
            //{
            //    //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cancelled");
            //    PhaseForColors = AestheticPhase.Cancelled;
            //    ((GH_ColorAttributes_Async)m_attributes).ColorSelected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.DarkRed);
            //    ((GH_ColorAttributes_Async)m_attributes).ColorUnselected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.DarkOrchid);
            //    logHelper.FinishTask(latestLogGuid, "Cancelled");
            //}
            //else
            //{
                PhaseForColors = AestheticPhase.NotRunning;

                if (RunInput)
                {
                    Message = "Ran in " + RunTime.ToShortString();


                }
                else
                {
                    Message = "Deactive";

                }
            //}

            //base.PostRunning();
        }

        protected override bool PreRunning(IGH_DataAccess DA)
        {

            RunInput = true;

            IGH_Param runParam = this.Params.Input.Where(o => o.NickName == "Run" && o.Access == GH_ParamAccess.tree).FirstOrDefault();

            if (runParam != null)
            {
                List<GH_Boolean> runInputs = DA.FetchTree<GH_Boolean>("Run").FlattenData();

                if (runInputs.Count == 0 || !runInputs.All(x => x.Value == true))
                {
                    RunInput = false;
                }
            }
            if (RunInput)
            {
                Stopwatch.Restart();
                //Stopwatch.Start();
                PhaseForColors = AestheticPhase.Running;
                ((GH_ColorAttributes_Async)m_attributes).ColorSelected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.MediumVioletRed);
                ((GH_ColorAttributes_Async)m_attributes).ColorUnselected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.Purple);
                Message = "Started";

            }
            else
            {
                PhaseForColors = AestheticPhase.Reusing;
                ((GH_ColorAttributes_Async)m_attributes).ColorSelected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.FromArgb(76, 128, 122));
                ((GH_ColorAttributes_Async)m_attributes).ColorUnselected = new Grasshopper.GUI.Canvas.GH_PaletteStyle(Color.FromArgb(95, 115, 113));
                Message = "Deactive";
            }
            //OnDisplayExpired(true);



            if (HasLogAbilities() && LogSave && RunInput)
            {
                
                //logHelper.Add($"{LogName} {RunCount - 1}", (LogUseFixedDescription ? LogDescriptionStatic : LogDescriptionDynamic) + " Starting", InstanceGuid);
                latestLogGuid = logHelper.AddTask($"{LogName} {RunCount - 1}", (LogUseFixedDescription ? LogDescriptionStatic : LogDescriptionDynamic), InstanceGuid);
            }

            return RunInput;


        }

        public override bool Read(GH_IReader reader)
        {
            string s = string.Empty;
            bool logSave = false;



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

            if (reader.TryGetBoolean("fireLogs", ref logSave))
            {
                LogSave = logSave;
            }

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {

            //writer.SetString("stdouts", String.Join(">JOIN<", ((SSH_Worker)BaseWorker).results.Select(r => r.Stdout)));

            writer.SetString("description", LogDescriptionDynamic);
            writer.SetString("staticDescription", LogDescriptionStatic);
            writer.SetString("name", LogName);

            writer.SetBoolean("fireLogs", LogSave);




            return base.Write(writer);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Cancel", (s, e) =>
            {
                RequestCancellation();
            });
        }

        public override void CreateAttributes()
        {
            //base.CreateAttributes();
            m_attributes = new GH_ColorAttributes_Async(this);

        }


        public override TimeSpan ProcessorTime => RunTime;
    }
}

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Renci.SshNet;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;
using MantaRay.Components;
using Rhino.Geometry;
using MantaRay.Setup;
using System.Runtime.InteropServices;
using MantaRay.Helpers;
using System.Threading.Tasks;
using MantaRay.Components.Templates;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Rhino.UI;
using Eto;
using MantaRay.Components.Views;
using Eto.Forms;
using MantaRay.Components.VM;
using Rhino.Commands;
using MantaRay.Components.Templates.Async;
using MantaRay.Interfaces;

namespace MantaRay.Components
{
    public class GH_Connect : GH_Template, ISetConnection, IHasDoubleClick
    {
        /// <summary>
        /// Initializes a new instance of the GH_Connect class.
        /// </summary>
        public GH_Connect()
          : base("Connect SSH/FTP", "Connect",
              "Connect to the SSH and FTP",
              "0 Setup")
        {
        }

        public bool WasConnected { get; set; } = false;

        private LoginVM LoginVM { get; set; }


        int connectID = 0;
        SSH_Helper sshHelper;
        SSH_Helper ISetConnection.SshHelper => sshHelper;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("user", "user", "input a string containing the linux user name.\nFor instance:\nmyName", GH_ParamAccess.item, System.Environment.UserName)].Optional = true;
            pManager[pManager.AddTextParameter("ip", "ip", "input a string containing the SSH ip address.\nFor instance:\n127.0.0.1\n\n" +
                "Also works for computer names on the network in case that you don't have a fixed IP.\n" +
                "For instance:\nmy-computer-042", GH_ParamAccess.item, "127.0.0.1")].Optional = true;
            pManager[pManager.AddTextParameter("LinuxDir", "LinuxDir", "Default linux dir.\nDefault is:\n'~/simulation'", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("WindowsDir", "WindowsDir", $"WindowsDir\nDefault is:\n'C:\\users\\{System.Environment.UserName}\\MantaRay\\", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("SftpDir", "SftpDir", "SftpDir. MantaRay transfers files over Sftp, which is standard protocol using SSH in SSH_Sharp.\n\n" +
                "This can in some cases be a windows directory even though you are SSH'ing to linux.\n" +
                "This is sometimes the case when using Windows Subsystem Linux" +
                "\nExamples:\n" +
                "'/C:/users/<username>/MantaRay'   ... (I know this is weird but that's how I've seen it with this SSH client)\n" +
                "'~/MantaRay/'", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("ProjectName", "ProjectName", "Subfolder for this project\n" +
                "If none is specified, files will land in UnnamedProject folder.\nIdeas:\nMyProject\nMyAwesomeProject", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("password", "password", "Password. Leave empty for the script to prompt on every connection.\n" +
                "This way your passwords are not saved in your grasshopper files. You could also point it toward a local file on your drive", GH_ParamAccess.item, "_prompt")].Optional = true;
            pManager[pManager.AddIntegerParameter("_port", "_port", "_port", GH_ParamAccess.item, 22)].Optional = true;
            connectID = pManager.AddBooleanParameter("connect", "connect", "Set to true to start connection. If you recompute the component it will reconnect using same password," +
                "however if you set connect to false, then it will remove the password.", GH_ParamAccess.item, false);
            pManager[connectID].Optional = true;
            pManager[pManager.AddTextParameter("prefixes", "Prefixes", "Prefixes can be used to set paths etc.This 'CAN' be executed on all components where 'use prefix' is set.\n" +
                "i added the prefixes because depending on the SSH setup then the paths may or may not be set up correctly\n" +
                "\n\nOn our local setup i added the file '/etc/profile' and added my paths to that.\n" +
                "In that case you enter to prefixes:   '. /etc/profile'", GH_ParamAccess.item, "")].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("status", "status", "status", GH_ParamAccess.item);
            pManager.AddGenericParameter("prefixes", "prefixes", "Show the current prefixes.\n\nThis means that all the execute components will run these commands before the actual command.\n\nIf you remove the prefix input, then this will output the default prefix!", GH_ParamAccess.item);
            pManager.AddTextParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            LoginVM = LoginVM ?? new LoginVM();

            TimingHelper th = null; // new TimingHelper("GH_Connect");
            sshHelper = sshHelper ?? new SSH_Helper();

            ManPageHelper.Initiate();
            bool run = DA.Fetch<bool>(this, "connect");
            WasConnected = false;




            // Moving to back will make sure this expires/runs before other objects when you load the file
            Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(this, GH_Arrange.MoveToBack);

            if (run && Grasshopper.Instances.ActiveCanvas.Document.Objects
                .OfType<GH_Connect>()
                .Where(c => !Object.ReferenceEquals(c, this) && !c.Locked)
                .Count() > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"There's more than one {this.NickName} component on the canvas.\n" +
                    $"One will override the other!\n" +
                    $"Please only use ONE! Do you get it???\n" +
                    $"For one to live the other one has to die\n" +
                    $"It's like Harry Potter and Voldemort.\n\nDisable the other component and enable this one again. Fool.");


                this.Locked = true;
                return;

            }
            th?.Benchmark("Checked other components");

            LoginVM.Username= DA.Fetch<string>(this, "user");
            string password = DA.Fetch<string>(this, "password");
            string linDir = DA.Fetch<string>(this, "LinuxDir");
            string winDir = DA.Fetch<string>(this, "WindowsDir");
            string sftpDir = DA.Fetch<string>(this, "SftpDir");
            string projectName = DA.Fetch<string>(this, "ProjectName", "ProjectName");
            LoginVM.Ip = DA.Fetch<string>(this, "ip");
            LoginVM.Port = DA.Fetch<int>(this, "_port");
            string prefixes = DA.Fetch<string>(this, "prefixes");

            

            StringBuilder sb = new StringBuilder();

            if (run)
            {
                Rhino.RhinoApp.WriteLine("MantaRay: Starting connect command. This may take a while especially if there is no command or wrong password...");

                if (password == "_prompt") //Default saved in the component
                {
                    if (LoginVM.Password == null)
                    {
                        if (!GetCredentials(LoginVM))
                            run = false;
                    }

                }
                
            }
            else
            {
                LoginVM.Password = null; //reset
                sshHelper.Disconnect();
                sshHelper = null;
            }

            th?.Benchmark("...password");


            if (run) // if its still on.. can be disabled above.
            {

                //Inspiration from https://gist.github.com/piccaso/d963331dcbf20611b094
                ConnectionInfo ConnNfo = new ConnectionInfo(
                    LoginVM.Ip, LoginVM.Port, LoginVM.Username,
                    new AuthenticationMethod[]
                    {

                        // Pasword based Authentication
                        new PasswordAuthenticationMethod(LoginVM.Username, LoginVM.Password),

                        //// Key Based Authentication (using keys in OpenSSH Format) Uncomment if you need the fingerprint!
                        //new PrivateKeyAuthenticationMethod(
                        //    username,
                        //    new PrivateKeyFile[]
                        //    {
                        //        new PrivateKeyFile(@"..\openssh.key","passphrase")
                        //    }
                        //)

                    }

                );




                Stopwatch stopwatch1 = new Stopwatch();
                Stopwatch stopwatch2 = new Stopwatch();
                //Connect SSH
                sshHelper.SshClient = new SshClient(ConnNfo);
                sshHelper.SshClient.ConnectionInfo.Timeout = new TimeSpan(0, 0, 10);

                th?.Benchmark("Create Client");

                if (!string.IsNullOrEmpty(winDir))
                {
                    sshHelper.WindowsParentPath = winDir;
                }
                else
                {
                    sshHelper.WindowsParentPath = SSH_Helper.DefaultWindowsParentPath;
                }



                if (!string.IsNullOrEmpty(linDir))
                {
                    sshHelper.LinuxParentPath = linDir;
                }
                else
                {
                    sshHelper.LinuxParentPath = SSH_Helper.DefaultLinuxParentPath;
                }






                if (!string.IsNullOrEmpty(projectName))
                {
                    sshHelper.ProjectSubPath = projectName;
                }
                else
                {
                    sshHelper.ProjectSubPath = SSH_Helper.DefaultProjectSubFolder;
                }

                StringBuilder sbSSH = new StringBuilder();



                th?.Benchmark("Setup Paths");

                //var ConnectSSH = Task.Factory.StartNew(() =>
                //{

                try
                {
                    sshHelper.HomeDirectory = null;
                    sshHelper.SshClient.Connect();

                    sbSSH.AppendFormat("SSH:  Connected in {0} ms\n", stopwatch1.ElapsedMilliseconds);


                }
                catch (Renci.SshNet.Common.SshAuthenticationException e)
                {
                    sbSSH.AppendLine("SSH: Connection Denied??\n" + e.Message);
                    var mb = MessageBox.Show("Wrong SSH Password? Wrong username? Try again?", "SSH Connection Denied", MessageBoxButtons.YesNo);
                    if (mb == DialogResult.Yes)
                    {
                        if (GetCredentials(LoginVM))
                        {
                       
                            this.ExpireSolution(true);
                        }

                    }
                    else
                    {
                        sb.Append("\nCancelled!");
                        DA.SetData("status", sb.ToString());
                        return;
                    }
                }



                catch (System.Net.Sockets.SocketException e)
                {
                    sbSSH.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                        "your bash with the command:\n    $ sudo service ssh start\n", e.Message);

                    if (String.Equals(LoginVM.Ip, "127.0.0.1") || String.Equals(LoginVM.Ip, "localhost"))
                    {
                        var mb = MessageBox.Show("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??" +
                            "\n\n\nI'll simply run the below bash command for you:\n\n" +
                            "C:\\windows\\system32\\cmd.exe\n\n" +
                            $"/c \"bash -c \"echo {{_pw}} | sudo -S service ssh start\" \"", "No SSH Found", MessageBoxButtons.YesNo);

                        if (mb == DialogResult.Yes)
                        {
                            Process proc = new System.Diagnostics.Process();
                            proc.StartInfo.FileName = @"C:\windows\system32\cmd.exe";
                            proc.StartInfo.Arguments = $"/c \"bash -c \"echo {LoginVM.Password} | sudo -S service ssh start\" \"";

                            proc.StartInfo.UseShellExecute = true;
                            proc.StartInfo.RedirectStandardOutput = false;

                            proc.Start();
                            proc.WaitForExit();

                            this.ExpireSolution(true);
                        }
                    }


                }
                catch (Exception e)
                {
                    sbSSH.AppendFormat("SSH:  {0}\n", e.Message);
                }
                sbSSH.Append("\n");

                //});

                th?.Benchmark("SSH Connected");



                StringBuilder sbFTP = new StringBuilder();

                //var ConnectSFTP = Task.Factory.StartNew(() =>
                //{
                stopwatch2.Restart();
                if (sshHelper.SshClient != null && sshHelper.SshClient.IsConnected)
                {

                    //Connect Sftp
                    sshHelper.SftpClient = new SftpClient(ConnNfo);
                    sshHelper.SshClient.ConnectionInfo.Timeout = new TimeSpan(0, 0, 10);
                    try
                    {

                        sshHelper.HomeDirectory = null;
                        sshHelper.SftpClient.Connect();

                        sbFTP.AppendFormat("Sftp: Connected in {0} ms\n", stopwatch2.ElapsedMilliseconds);

                    }
                    catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                    {
                        sbFTP.AppendLine("Sftp: Wrong password??\n" + e.Message);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        sbFTP.AppendFormat("Sftp: Could not find the Sftp server\n      {0}\n      Try restarting it locally in " +
                            "your bash with the command:\n    $ sudo service ssh start\n", e.Message);
                    }
                    catch (Exception e)
                    {
                        sbFTP.AppendFormat("Sftp: {0}\n", e.Message);
                    }

                    sbFTP.Append("\n");

                    if (!string.IsNullOrEmpty(sftpDir))
                    {
                        sshHelper.SftpHome = sftpDir + (sftpDir.Contains("/") ? "/" : "\\") + sshHelper.ProjectSubPath;
                    }
                    else
                    {

                        //sshHelper.SftpHome = sshHelper.SftpClient.WorkingDirectory;
                        sshHelper.SftpHome = sshHelper.LinuxHome;
                    }

                    sbFTP.AppendFormat("SSH:  Created server directory {0}\n\n", sshHelper.LinuxHome);

                }
                //});



                //try
                //{
                //    Task.WaitAll(new[] { ConnectSSH, ConnectSFTP });
                //    //{
                //    sb.Append(sbSSH);
                //    sb.Append(sbFTP);

                //    //}
                //    //else
                //    //{
                //    //    sb.Append("Attempt timed out (5000ms)");
                //    //}


                //}
                //catch (AggregateException ae)
                //{
                //    foreach (var e in ae.InnerExceptions)
                //    {
                //        throw e;
                //    }
                //}




                int pad = 45;


                sb.AppendFormat("SSH:  Set   <WinHome> to {0}\n", sshHelper.WinHome);
                sb.AppendFormat("SSH:  Set <LinuxHome> to {0}\n", sshHelper.LinuxHome);
                sb.AppendFormat("SSH:  Set   <Project> to {0}\n", sshHelper.ProjectSubPath);
                sb.AppendFormat("SSH:  Set  <SftpHome> to {0} This is used in the upload components\n", sshHelper?.SftpHome?.PadRight(pad, '.') ?? "");

                GlobalsHelper.GlobalsFromConnectComponent["WinHome"] = sshHelper.WinHome;
                GlobalsHelper.GlobalsFromConnectComponent["LinuxHome"] = sshHelper.LinuxHome;
                GlobalsHelper.GlobalsFromConnectComponent["Project"] = sshHelper.ProjectSubPath;
                GlobalsHelper.GlobalsFromConnectComponent["SftpHome"] = sshHelper.SftpHome;

                if (sshHelper.CheckConnection() == SSH_Helper.ConnectionDetails.Connected)
                {
                    string cpuSB = (int.Parse(sshHelper.Execute("nproc --all")) - 1).ToString();
                    GlobalsHelper.GlobalsFromConnectComponent["cpus"] = cpuSB;
                    sb.AppendFormat("SSH:  Set      <cpus> to {0} Locally you would have used {1}\n", cpuSB.PadRight(pad, '.'), (Environment.ProcessorCount - 1).ToString());

                }


                sshHelper.ExportPrefixes = string.IsNullOrEmpty(prefixes) ? sshHelper.ExportPrefixesDefault : prefixes.ApplyGlobals(GlobalsHelper.GlobalsFromConnectComponent);

                th?.Benchmark("SFTP connected2");

                WasConnected = sshHelper.SshClient.IsConnected && sshHelper.SftpClient.IsConnected;

                int relComponents = OnPingDocument().Objects
                    .OfType<GH_Template_Async_Extended>()
                    .Where(c => c.PhaseForColors == GH_Template_Async_Extended.AestheticPhase.Disconnected).Count();


                if (WasConnected && relComponents > 0)
                {
                    var mb = MessageBox.Show($"Rerun {relComponents} disconnected Execute components?", "Rerun expired components?", MessageBoxButtons.YesNo);
                    if (mb == DialogResult.Yes)
                    {
                        OnPingDocument().ScheduleSolution(20, UpdateAllExecutes);

                    }



                }



            }
            else
            {

                TryDisconnect();
                sb.Append("Sftp + SSH: Disconnected\n");
            }

            DA.SetData("status", sb.ToString());

            //the run output
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(sshHelper != null && sshHelper.CheckConnection() == SSH_Helper.ConnectionDetails.Connected));
            DA.SetData(1, sshHelper?.ExportPrefixes ?? "");
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (sshHelper == null || sshHelper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not Connected.\n\nTry restarting SSH in your bash with:\nsudo service ssh start");




        }

        public void UpdateAllExecutes(GH_Document doc)
        {

            foreach (var obj in OnPingDocument().Objects
                .OfType<GH_Template_Async_Extended>()
                .Where(c => c.PhaseForColors == GH_Template_Async_Extended.AestheticPhase.Disconnected))
            {
                obj.ExpireSolution(false);
            }

            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(5);


        }

        public void TryDisconnect()
        {
            sshHelper?.Disconnect();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            TryDisconnect();

            base.RemovedFromDocument(document);

        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (document.Objects.Contains(this))
            {

                var allData = this.Params.Input[connectID].VolatileData.AllData(false); // run input parameter
                bool isRunSet = allData.Count() > 0;


                if (Params.Input[connectID].Phase == GH_SolutionPhase.Blank)
                {
                    this.Params.Input[connectID].CollectData();

                }
                foreach (IGH_Goo data in allData)
                {
                    switch (data)
                    {
                        case GH_Boolean b:
                            if (!b.IsValid || b.Value == false) { isRunSet = false; }
                            break;
                        case GH_Integer @int:
                            if (!@int.IsValid || @int.Value == 0) { isRunSet = false; }
                            break;
                        case GH_Number num:
                            if (!num.IsValid || num.Value == 0) { isRunSet = false; }
                            break;
                        case GH_String text:
                            if (!text.IsValid || !string.Equals("true", text.Value, StringComparison.InvariantCultureIgnoreCase)) { isRunSet = false; }
                            break;
                        default:
                            isRunSet = false;
                            break;
                    }
                    if (!isRunSet)
                        break;
                }

                this.Params.Input[connectID].ClearData();

                if (isRunSet && sshHelper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
                {

                    document.ScheduleSolution(100, (e) => this.ExpireSolution(true));
                }


            }
            else
            {
                TryDisconnect();
            }


            base.DocumentContextChanged(document, context);
        }

        //public override void AddedToDocument(GH_Document document)
        //{

        //    Grasshopper.Instances.ActiveCanvas. -= ActiveCanvas_Disposed;
        //    Grasshopper.Instances.ActiveCanvas.Disposed += ActiveCanvas_Disposed;
        //    //TODO: Other events to subscribe to, to make sure to disconnect???

        //    base.AddedToDocument(document);
        //}

        //private void ActiveCanvas_Disposed(object sender, EventArgs e)
        //{
        //    TryDisconnect();
        //}


        protected override Bitmap Icon => Resources.Resources.Ra_Connect_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1B57442F-E5FE-4462-9EB0-564497CB076E"); }
        }

        public static void ReconnectIfNeeded()
        {
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(100, ReconnectIfNeeded);
        }

        public static GH_Connect GetActiveComponent(GH_Document doc)
        {
            return doc.Objects.OfType<GH_Connect>().FirstOrDefault(c => !c.Locked);
        }

        private static void ReconnectIfNeeded(GH_Document doc)
        {
            var comp = GetActiveComponent(doc);
            if (comp != null)
            {
                if (comp.WasConnected && (comp.sshHelper == null || comp.sshHelper.SshClient == null || !comp.sshHelper.SshClient.IsConnected || comp.sshHelper.SftpClient == null || !comp.sshHelper.SftpClient.IsConnected))
                {
                    comp.ExpireSolution(true);
                }

            }
        }

        private bool GetCredentials(LoginVM vm)
        {
            var form = new LoginView(vm);
            var rc = form.ShowModal(RhinoEtoApp.MainWindow);

            return rc == Result.Success;

        }



        //private bool GetCredentials2(string username, string ip, out string password)
        //{
        //    bool localIp = string.Equals(ip, "127.0.0.1") || string.Equals(ip, "localhost");
        //    var foreColor = localIp ? Color.FromArgb(88, 100, 84) : Color.FromArgb(128, 66, 19);
        //    var backColor = localIp ? Color.FromArgb(148, 180, 140) : Color.FromArgb(250, 205, 170);
        //    var background = localIp ? Color.FromArgb(255, 195, 195, 195) : Color.FromArgb(201, 165, 137);

        //    Font redFont = new Font("Arial", 18.0f,
        //                FontStyle.Bold);

        //    Font font = new Font("Arial", 10.0f,
        //                FontStyle.Bold);

        //    Font smallFont = new Font("Arial", 8.0f,
        //                FontStyle.Bold);

        //    Form prompt = new Form()
        //    {

        //        Width = 460,
        //        Height = 370,
        //        FormBorderStyle = FormBorderStyle.FixedDialog,
        //        Text = "Connect to SSH",
        //        StartPosition = FormStartPosition.CenterScreen,
        //        BackColor = background,
        //        ForeColor = Color.FromArgb(255, 30, 30, 30),
        //        Font = font

        //    };


        //    Label label = new Label()
        //    {
        //        Left = 50,
        //        Top = 45,
        //        Width = 340,
        //        Height = 28,
        //        Text = $"Connecting to SSH on {ip}:"
        //    };


        //    TextBox usernameTextBox = new TextBox()
        //    {
        //        Left = 50,
        //        Top = 75,
        //        Width = 340,
        //        Height = 28,
        //        Text = string.IsNullOrEmpty(username) ? "username" : username,
        //        ForeColor = foreColor,
        //        Font = redFont,
        //        BackColor = backColor,
        //        Enabled = false,
        //        Margin = new Padding(2)
        //    };


        //    TextBox passwordTextBox = new TextBox()
        //    {
        //        Left = 50,
        //        Top = 125,
        //        Width = 340,
        //        Height = 28,
        //        Text = "",
        //        ForeColor = foreColor,
        //        PasswordChar = '*',
        //        Font = redFont,
        //        BackColor = backColor,
        //        Margin = new Padding(2),

        //    };


        //    Button connectButton = new Button() { Text = "Connect", Left = 50, Width = 120, Top = 190, Height = 40, DialogResult = DialogResult.OK };
        //    Button cancel = new Button() { Text = "Cancel", Left = 270, Width = 120, Top = 190, Height = 40, DialogResult = DialogResult.Cancel };


        //    Label label2 = new Label()
        //    {
        //        Font = smallFont,
        //        Left = 50,
        //        Top = 270,
        //        Width = 340,
        //        Height = 60,
        //        Text = $"Part of the {ConstantsHelper.ProjectName} plugin\n" +
        //        "(C) Mathias Sønderskov Schaltz 2022"
        //    };
        //    prompt.Controls.AddRange(new Control[] { label, usernameTextBox, passwordTextBox, connectButton, cancel, label2 });

        //    if (usernameTextBox.Text != "username")
        //    {
        //        passwordTextBox.Focus();
        //    }

        //    prompt.AcceptButton = connectButton;


        //    DialogResult result = prompt.ShowDialog();

        //    if (result == DialogResult.OK)
        //    {
        //        password = passwordTextBox.Text;
        //        //_usr = usernameTextBox.Text;
        //        //outUsername = usernameTextBox.Text;
        //        return true;
        //    }
        //    else
        //    {
        //        password = null;
        //        //outUsername = null;
        //        return false;

        //    }
        //}

        public GH_ObjectResponse OnDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            this.ExpireSolution(true);
            return GH_ObjectResponse.Handled;
        }

        public override void CreateAttributes()
        {
            m_attributes = new GH_DoubleClickAttributes(this);

        }
    }
}
using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Renci.SshNet;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;
using Rhino.Geometry;
using MantaRay.Setup;

namespace MantaRay.OldComponents
{
    [Obsolete]
    public class GH_Connect_OBSOLETE : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_Connect class.
        /// </summary>
        public GH_Connect_OBSOLETE()
          : base("Connect SSH/FTP", "Connect",
              "Connect to the SSH and FTP",
              "0 Setup")
        {
        }

        private string _pw;
        int connectID = 0;

        SSH_Helper sshHelper;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("user", "user", "input a string containing the linux user name.\nFor instance:\nmyName", GH_ParamAccess.item, Environment.UserName)].Optional = true;
            pManager[pManager.AddTextParameter("ip", "ip", "input a string containing the SSH ip address.\nFor instance:\n127.0.0.1", GH_ParamAccess.item, "127.0.0.1")].Optional = true;
            pManager[pManager.AddTextParameter("LinuxDir", "LinuxDir", "Default linux dir.\nDefault is:\n'~/simulation'", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("WindowsDir", "WindowsDir", $"WindowsDir\nDefault is:\n'C:\\users\\{Environment.UserName}\\MantaRay\\", GH_ParamAccess.item, "")].Optional = true;
            //pManager[pManager.AddTextParameter("SftpDir", "SftpDir", "SftpDir. This can in some cases be a windows directory even though you are SSH'ing to linux.\nThis is sometimes the case when using Windows Subsystem Linux", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("ProjectName", "ProjectName", "Subfolder for this project\n" +
                "If none is specified, files will land in UnnamedProject folder.\nIdeas:\nMyProject\nMyAwesomeProject", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("password", "password", "password. Leave empty to prompt.", GH_ParamAccess.item, "_prompt")].Optional = true;
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
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
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

            ManPageHelper.Initiate();
            bool run = DA.Fetch<bool>(this, "connect");

            sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());


            // Moving to back will make sure this expires/runs before other objects when you load the file
            Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(this, GH_Arrange.MoveToBack);

            if (run && Grasshopper.Instances.ActiveCanvas.Document.Objects
                .OfType<GH_Connect_OBSOLETE>()
                .Where(c => !ReferenceEquals(c, this) && !c.Locked)
                .Count() > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"There's more than one {NickName} component on the canvas.\n" +
                    $"One will override the other!\n" +
                    $"Please only use ONE! Do you get it???\n" +
                    $"For one to live the other one has to die\n" +
                    $"It's like Harry Potter and Voldemort.\n\nDisable the other component and enable this one again. Fool.");


                Locked = true;
                return;

            }

            string username = DA.Fetch<string>(this, "user");
            string password = DA.Fetch<string>(this, "password");
            string linDir = DA.Fetch<string>(this, "LinuxDir");
            string winDir = DA.Fetch<string>(this, "WindowsDir");
            //string sftpDir = DA.Fetch<string>(this, "SftpDir");
            string subfolder = DA.Fetch<string>(this, "ProjectName", "Subfolder");
            string ip = DA.Fetch<string>(this, "ip");
            int port = DA.Fetch<int>(this, "_port");
            string prefixes = DA.Fetch<string>(this, "prefixes");

            StringBuilder sb = new StringBuilder();

            if (run)
            {


                if (password == "_prompt") //Default saved in the component
                {
                    if (_pw == null)
                    {
                        if (GetCredentials(username, ip, out string pw))
                        {
                            _pw = pw;


                        }

                        else
                            run = false;
                    }

                }
                else
                {
                    _pw = password;
                }

            }
            else
            {
                _pw = null; //reset
            }


            if (run)
            {

                //Inspiration from https://gist.github.com/piccaso/d963331dcbf20611b094
                ConnectionInfo ConnNfo = new ConnectionInfo(
                    ip, port, username,
                    new AuthenticationMethod[]
                    {

                        // Pasword based Authentication
                        new PasswordAuthenticationMethod(username, _pw),

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

                sshHelper.ExportPrefixes = string.IsNullOrEmpty(prefixes) ? sshHelper.ExportPrefixesDefault : prefixes;


                Stopwatch stopwatch = new Stopwatch();
                //Connect SSH
                sshHelper.SshClient = new SshClient(ConnNfo);

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



                if (!string.IsNullOrEmpty(subfolder))
                {
                    sshHelper.ProjectSubPath = subfolder;
                }
                else
                {
                    sshHelper.ProjectSubPath = SSH_Helper.DefaultProjectSubFolder;
                }







                try
                {
                    sshHelper.HomeDirectory = null;
                    sshHelper.SshClient.Connect();

                    sb.AppendFormat("SSH:  Connected in {0} ms\n", stopwatch.ElapsedMilliseconds);

                }
                catch (Renci.SshNet.Common.SshAuthenticationException e)
                {
                    sb.AppendLine("SSH: Connection Denied??\n" + e.Message);
                    var mb = MessageBox.Show("Wrong SSH Password? Wrong username? Try again?", "SSH Connection Denied", MessageBoxButtons.RetryCancel);
                    if (mb == DialogResult.Retry)
                    {
                        if (GetCredentials(username, ip, out string pw))
                        {
                            _pw = pw;
                            ExpireSolution(true);
                        }

                    }
                }


                catch (System.Net.Sockets.SocketException e)
                {
                    sb.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                        "your bash with the command:\n    $ sudo service ssh start\n", e.Message);

                    if (string.Equals(ip, "127.0.0.1") || string.Equals(ip, "localhost"))
                    {
                        var mb = MessageBox.Show("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??" +
                            "\n\n\nI'll simply run the below bash command for you:\n\n" +
                            "C:\\windows\\system32\\cmd.exe\n\n" +
                            $"/c \"bash -c \"echo {{_pw}} | sudo -S service ssh start\" \"", "No SSH Found", MessageBoxButtons.YesNo);

                        if (mb == DialogResult.Yes)
                        {
                            Process proc = new Process();
                            proc.StartInfo.FileName = @"C:\windows\system32\cmd.exe";
                            proc.StartInfo.Arguments = $"/c \"bash -c \"echo {_pw} | sudo -S service ssh start\" \"";

                            proc.StartInfo.UseShellExecute = true;
                            proc.StartInfo.RedirectStandardOutput = false;

                            proc.Start();
                            proc.WaitForExit();

                            ExpireSolution(true);
                        }
                    }


                }
                catch (Exception e)
                {
                    sb.AppendFormat("SSH:  {0}\n", e.Message);
                }


                sb.Append("\n");

                stopwatch.Restart();

                //Connect Sftp
                sshHelper.SftpClient = new SftpClient(ConnNfo);
                try
                {
                    sshHelper.HomeDirectory = null;
                    sshHelper.SftpClient.Connect();

                    sb.AppendFormat("Sftp: Connected in {0} ms\n", stopwatch.ElapsedMilliseconds);

                }
                catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                {
                    sb.AppendLine("Sftp: Wrong password??\n" + e.Message);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    sb.AppendFormat("Sftp: Could not find the Sftp server\n      {0}\n      Try restarting it locally in " +
                        "your bash with the command:\n    $ sudo service ssh start\n", e.Message);
                }
                catch (Exception e)
                {
                    sb.AppendFormat("Sftp: {0}\n", e.Message);
                }

                sb.Append("\n");
            }
            else
            {

                TryDisconnect();
                sb.Append("Sftp + SSH: Disconnected\n");
            }

            sshHelper.Execute($"mkdir -p {sshHelper.LinuxHome}");

            string cpuSB = (int.Parse(sshHelper.Execute("nproc --all")) - 1).ToString();

            sb.AppendFormat("SSH:  Created directory {0}\n\n", sshHelper.LinuxHome);


            sb.AppendFormat("SSH:  Setup <WinHome> to {0}\n", sshHelper.WinHome);
            sb.AppendFormat("SSH:  Setup <LinuxHome> to {0}\n", sshHelper.LinuxHome);
            sb.AppendFormat("SSH:  Setup <Project> to {0}\n", sshHelper.ProjectSubPath);
            sb.AppendFormat("SSH:  Setup <cpus> to {0} (locally you would have used {1})\n", cpuSB, (Environment.ProcessorCount - 1).ToString());

            GlobalsHelper.GlobalsFromConnectComponent["WinHome"] = sshHelper.WinHome;
            GlobalsHelper.GlobalsFromConnectComponent["LinuxHome"] = sshHelper.LinuxHome;
            GlobalsHelper.GlobalsFromConnectComponent["Project"] = sshHelper.ProjectSubPath;
            GlobalsHelper.GlobalsFromConnectComponent["cpus"] = cpuSB;


            DA.SetData("status", sb.ToString());

            //the run output
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(sshHelper.CheckConnection() == SSH_Helper.ConnectionDetails.Connected));
            DA.SetData(1, sshHelper.ExportPrefixes);
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (sshHelper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not Connected.\n\nTry restarting SSH in your bash with:\nsudo service ssh start");




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

                var allData = Params.Input[connectID].VolatileData.AllData(false);
                bool isRunSet = allData.Count() > 0;


                if (Params.Input[connectID].Phase == GH_SolutionPhase.Blank)
                {
                    Params.Input[connectID].CollectData();

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

                Params.Input[connectID].ClearData();

                if (isRunSet && sshHelper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
                {

                    document.ScheduleSolution(100, (e) => ExpireSolution(true));
                }


            }
            else
            {
                TryDisconnect();
            }


            base.DocumentContextChanged(document, context);
        }

        public override void AddedToDocument(GH_Document document)
        {

            Grasshopper.Instances.ActiveCanvas.Disposed -= ActiveCanvas_Disposed;
            Grasshopper.Instances.ActiveCanvas.Disposed += ActiveCanvas_Disposed;
            //TODO: Other events to subscribe to, to make sure to disconnect???

            base.AddedToDocument(document);
        }

        private void ActiveCanvas_Disposed(object sender, EventArgs e)
        {
            TryDisconnect();
        }


        protected override Bitmap Icon => Resources.Resources.Ra_Connect_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1B57442F-E5FE-4462-9EB0-564497CB076D"); }
        }

        private bool GetCredentials(string username, string ip, out string password)
        {
            bool localIp = string.Equals(ip, "127.0.0.1") || string.Equals(ip, "localhost");
            var foreColor = localIp ? Color.FromArgb(88, 100, 84) : Color.FromArgb(128, 66, 19);
            var backColor = localIp ? Color.FromArgb(148, 180, 140) : Color.FromArgb(250, 205, 170);
            var background = localIp ? Color.FromArgb(255, 195, 195, 195) : Color.FromArgb(201, 165, 137);

            Font redFont = new Font("Arial", 18.0f,
                        FontStyle.Bold);

            Font font = new Font("Arial", 10.0f,
                        FontStyle.Bold);

            Font smallFont = new Font("Arial", 8.0f,
                        FontStyle.Bold);

            Form prompt = new Form()
            {
                Width = 460,
                Height = 370,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Connect to SSH",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = background,
                ForeColor = Color.FromArgb(255, 30, 30, 30),
                Font = font

            };


            Label label = new Label()
            {
                Left = 50,
                Top = 45,
                Width = 340,
                Height = 28,
                Text = $"Connecting to SSH on {ip}:"
            };


            TextBox usernameTextBox = new TextBox()
            {
                Left = 50,
                Top = 75,
                Width = 340,
                Height = 28,
                Text = string.IsNullOrEmpty(username) ? "username" : username,
                ForeColor = foreColor,
                Font = redFont,
                BackColor = backColor,
                Enabled = false,
                Margin = new Padding(2)
            };


            TextBox passwordTextBox = new TextBox()
            {
                Left = 50,
                Top = 125,
                Width = 340,
                Height = 28,
                Text = "",
                ForeColor = foreColor,
                PasswordChar = '*',
                Font = redFont,
                BackColor = backColor,
                Margin = new Padding(2),

            };


            Button connectButton = new Button() { Text = "Connect", Left = 50, Width = 120, Top = 190, Height = 40, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 270, Width = 120, Top = 190, Height = 40, DialogResult = DialogResult.Cancel };


            Label label2 = new Label()
            {
                Font = smallFont,
                Left = 50,
                Top = 270,
                Width = 340,
                Height = 60,
                Text = $"Part of the {ConstantsHelper.ProjectName} plugin\n" +
                "(C) Mathias Sønderskov Schaltz 2022"
            };
            prompt.Controls.AddRange(new Control[] { label, usernameTextBox, passwordTextBox, connectButton, cancel, label2 });

            if (usernameTextBox.Text != "username")
            {
                passwordTextBox.Focus();
            }

            prompt.AcceptButton = connectButton;


            DialogResult result = prompt.ShowDialog();

            if (result == DialogResult.OK)
            {
                password = passwordTextBox.Text;
                //_usr = usernameTextBox.Text;
                //outUsername = usernameTextBox.Text;
                return true;
            }
            else
            {
                password = null;
                //outUsername = null;
                return false;

            }
        }
    }
}
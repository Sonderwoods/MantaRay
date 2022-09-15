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
using MantaRay.Components;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_Connect : GH_Template
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

        private string _pw;
 

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddTextParameter("user", "user", "input a string containing the linux user name.\nFor instance:\nmyName", GH_ParamAccess.item, System.Environment.UserName)].Optional = true;
            pManager[pManager.AddTextParameter("ip", "ip", "input a string containing the SSH ip address.\nFor instance:\n127.0.0.1", GH_ParamAccess.item, "127.0.0.1")].Optional = true;
            pManager[pManager.AddTextParameter("LinuxDir", "LinuxDir", "LinuxDir", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("WindowsDir", "WindowsDir", "WindowsDir", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("Subfolder", "Subfolder", "Subfolder", GH_ParamAccess.item, "")].Optional = true;
            pManager[pManager.AddTextParameter("password", "password", "password. Leave empty to prompt.", GH_ParamAccess.item, "_prompt")].Optional = true;
            pManager[pManager.AddIntegerParameter("_port", "_port", "_port", GH_ParamAccess.item, 22)].Optional = true;
            pManager[pManager.AddBooleanParameter("connect", "connect", "Set to true to start connection. If you recompute the component it will reconnect using same password," +
                "however if you set connect to false, then it will remove the password.", GH_ParamAccess.item, false)].Optional = true;
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

            ManPageHelper.Initiate();
            bool run = DA.Fetch<bool>("connect");


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

            string username = DA.Fetch<string>("user");
            string password = DA.Fetch<string>("password");
            string linDir = DA.Fetch<string>("LinuxDir");
            string winDir = DA.Fetch<string>("WindowsDir");
            string subfolder = DA.Fetch<string>("Subfolder");
            string ip = DA.Fetch<string>("ip");
            int port = DA.Fetch<int>("_port");
            string prefixes = DA.Fetch<string>("prefixes");

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

                SSH_Helper.ExportPrefixes = string.IsNullOrEmpty(prefixes) ? SSH_Helper.ExportPrefixesDefault : prefixes;


                Stopwatch stopwatch = new Stopwatch();
                //Connect SSH
                SSH_Helper.SshClient = new SshClient(ConnNfo);

                if (!string.IsNullOrEmpty(winDir))
                {
                    SSH_Helper.WindowsParentPath = System.IO.Path.GetDirectoryName(winDir);
                }
                else
                {
                    SSH_Helper.WindowsParentPath = SSH_Helper.DefaultWindowsParentPath;
                }

                if (!string.IsNullOrEmpty(linDir))
                {
                    SSH_Helper.LinuxParentPath = System.IO.Path.GetDirectoryName(linDir);
                }
                else
                {
                    SSH_Helper.LinuxParentPath = SSH_Helper.DefaultLinuxParentPath;
                }

                if (!string.IsNullOrEmpty(subfolder))
                {
                    SSH_Helper.DefaultSubfolder = subfolder;
                }
                else
                {
                    SSH_Helper.DefaultSubfolder = SSH_Helper.DefaultDefaultSubfolder;
                }

                sb.AppendFormat("SSH:  Setup <WinHome> to {0}\n", SSH_Helper.WindowsFullpath);
                sb.AppendFormat("SSH:  Setup <LinuxHome> to {0}\n", SSH_Helper.LinuxFullpath);

                

                try
                {
                    SSH_Helper.HomeDirectory = null;
                    SSH_Helper.SshClient.Connect();

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
                            this.ExpireSolution(true);
                        }
                        
                    }
                }

                
                catch (System.Net.Sockets.SocketException e)
                {
                    sb.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                        "your bash with the command:\n    $ sudo service ssh start\n", e.Message);

                    if (String.Equals(ip, "127.0.0.1") || String.Equals(ip, "localhost"))
                    {
                        var mb = MessageBox.Show("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??" +
                            "\n\n\nI'll simply run the below bash command for you:\n\n" +
                            "C:\\windows\\system32\\cmd.exe\n\n" +
                            $"/c \"bash -c \"echo {{_pw}} | sudo -S service ssh start\" \"", "No SSH Found", MessageBoxButtons.YesNo);

                        if (mb == DialogResult.Yes)
                        {
                            Process proc = new System.Diagnostics.Process();
                            proc.StartInfo.FileName = @"C:\windows\system32\cmd.exe";
                            proc.StartInfo.Arguments = $"/c \"bash -c \"echo {_pw} | sudo -S service ssh start\" \"";

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
                    sb.AppendFormat("SSH:  {0}\n", e.Message);
                }


                sb.Append("\n");

                stopwatch.Restart();

                //Connect Sftp
                SSH_Helper.SftpClient = new SftpClient(ConnNfo);
                try
                {
                    SSH_Helper.HomeDirectory = null;
                    SSH_Helper.SftpClient.Connect();

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


            DA.SetData("status", sb.ToString());

            //the run output
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(SSH_Helper.CheckConnection() == SSH_Helper.ConnectionDetails.Connected));
            DA.SetData(1, SSH_Helper.ExportPrefixes);
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (SSH_Helper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not Connected.\n\nTry restarting SSH in your bash with:\nsudo service ssh start");




        }



        public void TryDisconnect()
        {
            SSH_Helper.Disconnect();
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
                document.ScheduleSolution(100, (e) => this.ExpireSolution(true));

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
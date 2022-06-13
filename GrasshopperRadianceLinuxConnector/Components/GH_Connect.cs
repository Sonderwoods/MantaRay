using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Renci.SshNet;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;

namespace GrasshopperRadianceLinuxConnector.Components
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
            pManager[pManager.AddBooleanParameter("connect", "connect", "connect", GH_ParamAccess.item, false)].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("status", "status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (Grasshopper.Instances.ActiveCanvas.Document.Objects.OfType<GH_Connect>().Where(c => !Object.ReferenceEquals(c, this)).Count() > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"There's more than one {this.NickName} component on the canvas.\n" +
                    $"One will override the other!\n" +
                    $"Please only use ONE! Do you get it???\n" +
                    $"For one to live the other one has to die\n" +
                    $"It's like Harry Potter and Voldemort.\n\nDisable the other component and enable this one again. Fool.");

                if (Grasshopper.Instances.ActiveCanvas.Document.Objects.
                    OfType<GH_Connect>().
                    Where(c => c.Locked != true).
                    Where(c => !Object.ReferenceEquals(c, this)).
                    Count() > 0)
                {
                    this.Locked = true;
                    return;

                }


            }
            Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(this, GH_Arrange.MoveToBack);



            string username = DA.Fetch<string>("user");
            string password = DA.Fetch<string>("password");
            string linDir = DA.Fetch<string>("LinuxDir");
            string winDir = DA.Fetch<string>("WindowsDir");
            string subfolder = DA.Fetch<string>("Subfolder");
            string ip = DA.Fetch<string>("ip");
            int port = DA.Fetch<int>("_port");
            bool run = DA.Fetch<bool>("connect");

            StringBuilder sb = new StringBuilder();

            if (run)
            {

                if (password == "_prompt") //Default saved in the component
                {
                    if (_pw == null)
                    {
                        if (GetPassword(username, out string pw))
                            _pw = pw;
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
                Stopwatch stopwatch = new Stopwatch();
                //Connect SSH
                SSH_Helper.SshClient = new SshClient(ConnNfo);

                if (!string.IsNullOrEmpty(winDir))
                {
                    SSH_Helper.WindowsParentPath = System.IO.Path.GetDirectoryName(winDir);
                }

                if (!string.IsNullOrEmpty(linDir))
                {
                    SSH_Helper.LinuxParentPath = System.IO.Path.GetDirectoryName(linDir);
                }

                if (!string.IsNullOrEmpty(subfolder))
                {
                    SSH_Helper.DefaultSubfolder = subfolder;
                }

                sb.AppendFormat("SSH:  Setup windows folder to {0}\n", SSH_Helper.WindowsFullpath);
                sb.AppendFormat("SSH:  Setup linux folder to {0}\n", SSH_Helper.LinuxFullpath);


                try
                {
                    SSH_Helper.SshClient.Connect();

                    sb.AppendFormat("SSH:  Connected in {0} ms\n", stopwatch.ElapsedMilliseconds);

                }
                catch (Renci.SshNet.Common.SshAuthenticationException e)
                {
                    sb.AppendLine("SSH:  Wrong password??\n" + e.Message);
                    var mb = MessageBox.Show("Wrong SSH Password? Try again?", "Wrong SSH Password? Try again?", MessageBoxButtons.RetryCancel);
                    if (mb == DialogResult.Retry)
                    {
                        if (GetPassword(username, out string pw))
                            _pw = pw;
                        this.ExpireSolution(true);
                    }
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    sb.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                        "your bash with the command:\n    $ sudo service ssh start\n", e.Message);
                    var mb = MessageBox.Show("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??", "No SSH?", MessageBoxButtons.YesNo);

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
                    SSH_Helper.SftpClient.Connect();
                    //SSH_Helper.SetExtendedUserDir();

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
            TryDisconnect();

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




        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1B57442F-E5FE-4462-9EB0-564497CB076D"); }
        }

        private bool GetPassword(string username, out string password)
        {

            Font redFont = new Font("Arial", 18.0f,
                        FontStyle.Bold);

            Font font = new Font("Arial", 10.0f,
                        FontStyle.Bold);


            Form prompt = new Form()
            {
                Width = 400,
                Height = 270,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Connect to SSH",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(255, 185, 185, 185),
                ForeColor = Color.FromArgb(255, 30, 30, 30),
                Font = font
                
            };
            
            

            Label label = new Label() { Left = 50, Top = 35, Width=300, Height = 60, Text = $"Connecting to SSH\nInsert password for {username}:" };
            TextBox passwordTextBox = new TextBox() { Left = 50, Top = 95, Width = 300, Height = 30, Text = "",
                ForeColor = Color.FromArgb(255, 230, 45, 14),
                PasswordChar = '*',
                Font = redFont,
                BackColor = Color.FromArgb(255, 35, 25, 20),
                Margin = new Padding(2)
            };
            



            Button connectButton = new Button() { Text = "Connect", Left = 50, Width = 100, Top = 150, Height = 40, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 250, Width = 100, Top = 150, Height = 40, DialogResult = DialogResult.Cancel };

            prompt.Controls.AddRange(new Control[] { label, passwordTextBox, connectButton, cancel });

            prompt.AcceptButton = connectButton;

            DialogResult result = prompt.ShowDialog();

            if (result == DialogResult.OK)
            {
                password = passwordTextBox.Text;
                return true;
            }
            else
            {
                password = null;
                return false;

            }
        }
    }
}
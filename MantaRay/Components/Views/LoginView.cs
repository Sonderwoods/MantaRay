using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using MantaRay.Components.VM;
using Rhino.UI;
using System.Text;
using Renci.SshNet;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using Eto.Wpf;
using MantaRay.Components.Controls;
using MantaRay.Helpers;
using System.ComponentModel;

namespace MantaRay.Components.Views
{
    public class LoginView : Dialog<Rhino.Commands.Result>
    {

        protected TextBox IpTextBox { get; set; }
        protected TextBox UserNameTextBox { get; set; }
        protected PasswordBox PasswordBox { get; set; }
        protected Label StatusLabel { get; set; }

        protected Drawable CButton { get; set; }
        
     

        protected LoginVM ViewModel => DataContext as LoginVM;



        public LoginView(LoginVM vm)
        {

            DataContext = vm;
            var x = vm.GetType().Assembly.GetName().FullName;

            var y = x;


            XamlReader.Load(this);


            SetupStyles();

            SetupBinding();

            UpdateColors();

            PasswordBox.Focus();

            PasswordBox.KeyDown += PasswordBox_KeyDown;

            Invalidate();


        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Keys.Enter)
            {
                SaveAndClose();
            }
        }

        private void SetupStyles()
        {
            Eto.Style.Add<Eto.Wpf.Forms.Controls.ButtonHandler>(null, h =>
            {
                h.Control.BorderThickness = new System.Windows.Thickness(3);
                h.Control.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 100, 30));
                

            });
        }

        private void SetupBinding()
        {
            AbortButton.Click += AbortButton_Click;
            DefaultButton.Click += DefaultButton_Click;
            ((CustomButton)CButton).Click += TestButton_Click;
        }

        private void DefaultButton_Click(object sender, EventArgs e)
        {
            SaveAndClose();
        }

        private void SaveAndClose()
        {
            ViewModel.Ip = IpTextBox.Text.Split(':').First();
            ViewModel.Port = int.Parse(IpTextBox.Text.Split(':').Last());
            ViewModel.Username = UserNameTextBox.Text;
            ViewModel.Password = PasswordBox.Text;
            Close(Rhino.Commands.Result.Success);
        }

        private void AbortButton_Click(object sender, EventArgs e)
        {
            Close(Rhino.Commands.Result.Cancel);

        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            // TODO: Need to look into PropertyNotified  and more async stuff + ui updates


            Stopwatch stopwatch1 = new Stopwatch();

            string ip = IpTextBox.Text.Split(':').First();
            int port = int.Parse(IpTextBox.Text.Split(':').Last());
            string username = UserNameTextBox.Text;
            string password = PasswordBox.Text;


            // TODO: Need to add below to the ssh helper class and not view
            StringBuilder sbSSH = new StringBuilder();


            ConnectionInfo ConnNfo = new ConnectionInfo(
                    ip, port, username,
                    new AuthenticationMethod[]
                    { new PasswordAuthenticationMethod(username, password),}
                );

            SSH_Helper sshHelper = new SSH_Helper();
            sshHelper.SshClient = new SshClient(ConnNfo);
            sshHelper.SshClient.ConnectionInfo.Timeout = new TimeSpan(0, 0, 10);

            try
            {
                sshHelper.HomeDirectory = null;
                sshHelper.SshClient.Connect();
                sbSSH.AppendFormat("SSH:  Connected in {0} ms\n", stopwatch1.ElapsedMilliseconds);


            }
            catch (Renci.SshNet.Common.SshAuthenticationException ee)
            {
                sbSSH.AppendLine("SSH: Connection Denied??\n" + ee.Message);
                sbSSH.AppendFormat("Wrong SSH Password? Wrong username? Try again?\n");

            }

            catch (System.Net.Sockets.SocketException ee)
            {
                sbSSH.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                    "your bash with the command:\n    $ sudo service ssh start\n", ee.Message);

                if (String.Equals(ip, "127.0.0.1") || String.Equals(ip, "localhost"))
                {
                    sbSSH.Append("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??" +
                        "\n\n\nI'll simply run the below bash command for you:\n\n" +
                        "C:\\windows\\system32\\cmd.exe\n\n" +
                        $"/c \"bash -c \"echo {{_pw}} | sudo -S service ssh start\" \"");


                }


            }
            catch (Exception ee)
            {
                sbSSH.AppendFormat("SSH:  {0}\n", ee.Message);
            }

            StatusLabel.Text = sbSSH.ToString();
        }

        protected void HandleIpChanged(object sender, EventArgs e)
        {
            Rhino.RhinoApp.WriteLine("Updated Test..");

            UpdateColors();

        }

      


        protected void UpdateColors()
        {
            if (IpTextBox is null) { return; }


            bool IsLocalIp = ViewModel.LocalIp(IpTextBox.Text);

            Color foregroundColor = IsLocalIp ? Color.FromArgb(88, 100, 84) : Color.FromArgb(128, 66, 19);
            Color backColor = IsLocalIp ? Color.FromArgb(148, 180, 140) : Color.FromArgb(250, 205, 170);
            Color backGroundColor = IsLocalIp ? Color.FromArgb(255, 195, 195, 195) : Color.FromArgb(201, 165, 137);

            IpTextBox.BackgroundColor = backColor;
            IpTextBox.TextColor = foregroundColor;

            UserNameTextBox.BackgroundColor = backColor;
            UserNameTextBox.TextColor = foregroundColor;

            PasswordBox.BackgroundColor = backColor;
            PasswordBox.TextColor = foregroundColor;

            BackgroundColor = backGroundColor;


            //Invalidate();
        }


    }
}

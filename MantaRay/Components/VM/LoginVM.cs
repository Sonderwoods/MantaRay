using MantaRay.Helpers;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace MantaRay.Components.VM
{
    public class LoginVM : INotifyPropertyChanged
    {

        public SSH_Helper SSH_Helper { get; set; }
        public string Ip { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public int Port { get; set; }

        public string ConnectionStatus
        {
            get => connectionStatus;
            set
            {
                if (connectionStatus != value)
                {
                    connectionStatus = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ConnectionStatus)));
                }
            }
        }

        string connectionStatus = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public bool LocalIp(string ip) => string.Equals(ip, "127.0.0.1") || string.Equals(ip, "localhost");

        public NotifyTaskCompletion<string> old_ConnectionStatus { get; private set; }

        public void Connect()
        {
            old_ConnectionStatus = new NotifyTaskCompletion<string>(_Connect());

        }

        private void SetStatus(string status)
        {
            Eto.Forms.Application.Instance.Invoke(() => { ConnectionStatus = status; });
        }

        private async Task<string> _Connect()
        {
            StringBuilder sbSSH = new StringBuilder();

            await Task.Run(() =>
            {

                Stopwatch stopwatch1 = new Stopwatch();


                // TODO: Need to add below to the ssh helper class and not view



                ConnectionInfo ConnNfo = new ConnectionInfo(
                        Ip, Port, Username,
                        new AuthenticationMethod[]
                        { new PasswordAuthenticationMethod(Username, Password),}
                    );

                SSH_Helper sshHelper = new SSH_Helper();
                sshHelper.SshClient = new SshClient(ConnNfo);
                sshHelper.SshClient.ConnectionInfo.Timeout = new TimeSpan(0, 0, 10);

                SetStatus("Connecting...");

                try
                {
                    sshHelper.HomeDirectory = null;
                    sshHelper.SshClient.Connect();
                    sbSSH.AppendFormat("SSH:  Connected in {0} ms\n", stopwatch1.ElapsedMilliseconds);
                    SetStatus("Connected");


                }
                catch (Renci.SshNet.Common.SshAuthenticationException ee)
                {
                    sbSSH.AppendLine("SSH: Connection Denied??\n" + ee.Message);
                    sbSSH.AppendFormat("Wrong SSH Password? Wrong username? Try again?\n");
                    SetStatus("Wrong Username/Password");

                }

                catch (System.Net.Sockets.SocketException ee)
                {
                    sbSSH.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                        "your bash with the command:\n    $ sudo service ssh start\n", ee.Message);

                    if (String.Equals(Ip, "127.0.0.1") || String.Equals(Ip, "localhost"))
                    {
                        sbSSH.Append("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??" +
                            "\n\n\nI'll simply run the below bash command for you:\n\n" +
                            "C:\\windows\\system32\\cmd.exe\n\n" +
                            $"/c \"bash -c \"echo {{_pw}} | sudo -S service ssh start\" \"");


                    }

                    SetStatus("Could not find the host");


                }
                catch (Exception ee)
                {
                    sbSSH.AppendFormat("SSH:  {0}\n", ee.Message);

                    SetStatus(ee.Message);
                }


            });



            //// Artificial delay to show responsiveness.
            //await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

            //// Download the actual data and count it.
            //using (var client = new HttpClient())
            //{
            //    var data = await client.GetByteArrayAsync(url).ConfigureAwait(false);
            //    return data.Length;
            //}

            return ConnectionStatus;
        }




    }
}

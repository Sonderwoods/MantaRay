using MantaRay.Helpers;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MantaRay.Components.VM
{
    public class LoginVM : INotifyPropertyChanged
    {

        public SSH_Helper SSH_Helper { get; set; }
        public string Ip { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public int Port { get; set; }

        // TODO: Need to create a command and enable to check if any connections are ongoing (OR if we changed some details)
        public bool CanTestNewConnection
        {
            get => canTestNewConnection;
            set
            {
                if (canTestNewConnection != value)
                {
                    canTestNewConnection = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(CanTestNewConnection)));

                }

            }
        }

        bool canTestNewConnection = true;

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

        public bool IsOk
        {
            get => isOk;
            set
            {
                if (isOk != value)
                {
                    isOk = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsOk)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsFailed)));

                }

            }
        }

        bool isOk = true;

        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged();


                }

            }
        }

        bool isConnected;


        

        public bool IsFailed
        {
            get => !isOk;

        }

        string connectionStatus = "";

        

        public event PropertyChangedEventHandler PropertyChanged;


        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public bool LocalIp(string ip) => string.Equals(ip.Split(':').First(), "127.0.0.1") || string.Equals(ip.Split(':').First(), "localhost");


        public NotifyTaskCompletion<string> old_ConnectionStatus { get; private set; }

        public void Connect(Action done = null)
        {

            old_ConnectionStatus = new NotifyTaskCompletion<string>(connect(done, IsOk));

        }

        private void SetStatus(string status, bool? isOk = null, bool? connectionInProgress = null, bool? isConnected = null, bool? canTestNewConnection = null)
        {
            Eto.Forms.Application.Instance.Invoke(() => { ConnectionStatus = status; });
            if (isOk.HasValue)
            {

                Eto.Forms.Application.Instance.Invoke(() => { IsOk = isOk.Value; });
            }
            if (connectionInProgress.HasValue)
            {

                Eto.Forms.Application.Instance.Invoke(() => { CanTestNewConnection = connectionInProgress.Value; });
            }

            if (isConnected.HasValue)
            {

                Eto.Forms.Application.Instance.Invoke(() => { IsConnected = isConnected.Value; });
            }

            if (canTestNewConnection.HasValue)
            {

                Eto.Forms.Application.Instance.Invoke(() => { CanTestNewConnection = canTestNewConnection.Value; });
            }
        }



        private async Task<string> connect(Action Done = null, bool? success = null)
        {
            //StringBuilder sbSSH = new StringBuilder();


            SSH_Helper = null;



            await Task.Run(() =>
            {


                Stopwatch stopwatch1 = new Stopwatch();


                // TODO: Need to add below to the ssh helper class and not view

                ConnectionInfo ConnNfo = new ConnectionInfo
                (
                    Ip, Port, Username,
                    new AuthenticationMethod[] { new PasswordAuthenticationMethod(Username, Password) }
                );

                SSH_Helper sshHelper = new SSH_Helper
                {
                    SshClient = new SshClient(ConnNfo)
                };

                sshHelper.SshClient.ConnectionInfo.Timeout = new TimeSpan(0, 0, 5);

                SetStatus("Connecting...", true, true, false, false);

                try
                {
                    sshHelper.HomeDirectory = null;
                    sshHelper.SshClient.Connect();
                    //sbSSH.AppendFormat("SSH:  Connected in {0} ms\n", stopwatch1.ElapsedMilliseconds);
                    if (sshHelper.CheckConnection() == SSH_Helper.ConnectionDetails.Connected)
                    {
                        SetStatus($"Connected in {stopwatch1.ElapsedMilliseconds} ms", true, false, true);

                    }
                    else
                    {
                        SetStatus("Not Connected", false, false, false);
                    }


                }
                catch (Renci.SshNet.Common.SshAuthenticationException ee)
                {
                    //sbSSH.AppendLine("SSH: Connection Denied??\n" + ee.Message);
                    //sbSSH.AppendFormat("Wrong SSH Password? Wrong username? Try again?\n");
                    SetStatus("Connection Denied. Wrong Username/Password?\n" + ee.Message, false, false);

                }

                catch (System.Net.Sockets.SocketException ee)
                {
                    //sbSSH.AppendFormat("SSH:  Could not find the SSH server\n      {0}\n      Try restarting it locally in " +
                    //    "your bash with the command:\n    $ sudo service ssh start\n", ee.Message);

                    //if (String.Equals(Ip, "127.0.0.1") || String.Equals(Ip, "localhost"))
                    //{
                    //    sbSSH.Append("No SSH, try opening it with\nsudo service ssh start\n\nWant me to start it for you??" +
                    //        "\n\n\nI'll simply run the below bash command for you:\n\n" +
                    //        "C:\\windows\\system32\\cmd.exe\n\n" +
                    //        $"/c \"bash -c \"echo {{_pw}} | sudo -S service ssh start\" \"");


                    //}

                    SetStatus($"Could not find the host {Ip}:{Port}\n{ee.Message}", false, false);


                }
                catch (Exception ee)
                {
                    //sbSSH.AppendFormat("SSH:  {0}\n", ee.Message);

                    SetStatus(ee.Message, false, false);
                }

                Eto.Forms.Application.Instance.Invoke(() =>
                {
                    SSH_Helper = sshHelper;
                    if(Done != null && (success ?? false))
                    {
                        Done();
                    }

                });






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

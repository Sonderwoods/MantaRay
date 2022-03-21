using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;


namespace GrasshopperRadianceLinuxConnector
{
    internal static class SSH_Helper
    {
        public static SshClient SshClient
        {
            get => sshClient;
            set
            {
                if (sshClient == null)
                    sshClient = value;

                else
                {
                    sshClient.Disconnect();
                    sshClient.Dispose();
                    sshClient = value;

                }

            }
        }

        public static string HomeDirectory { get; set; } = null;

        private static SshClient sshClient;


        public static SftpClient SftpClient
        {
            get => sftpClient;
            set
            {
                if (sftpClient == null)
                {
                    sftpClient = value;
                    sftpClient.BufferSize = 4096; // bypass Payload error large files https://gist.github.com/DavidDeSloovere/96f3a827b54f20d52bcfda4fe7a16a0b
                    
                }

                else
                {
                    sftpClient.Disconnect();
                    sftpClient.Dispose();
                    sftpClient = value;

                }

            }
        }

        private static SftpClient sftpClient;


        public static void Upload(string localFileName, string SshPath = "~/simulation", StringBuilder sb = null)
        {

            if (SSH_Helper.SftpClient != null && SSH_Helper.SftpClient.IsConnected)
            {
                try
                {
                    HomeDirectory = HomeDirectory ?? sftpClient.WorkingDirectory;
                    //SshPath = SshPath.Replace("~", HomeDirectory);
                    //SSH_Helper.SftpClient.ChangeDirectory(SshPath);
                    SSH_Helper.SftpClient.ChangeDirectory(HomeDirectory);
                }
                catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                {
                    throw new Renci.SshNet.Common.SftpPathNotFoundException($"Linux Path not found {e.Message}.\nTry {HomeDirectory}\nThe current working directory is {sftpClient.WorkingDirectory}");
                }

                if (!File.Exists(localFileName))
                    throw new FileNotFoundException("Local file not found: " + localFileName);


                using (var uplfileStream = File.OpenRead(localFileName))
                {
                    SSH_Helper.SftpClient.UploadFile(uplfileStream, Path.GetFileName(localFileName), true);
                }

                //SSH_Helper.Execute($"cd ~;mv {Path.GetFileName(localFileName)} {SshPath}", sb);


            }
            else if (SSH_Helper.SftpClient != null)
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is a Sftp client but no connection. Please run the Connect SSH Component");
            }
            else
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is no Sftp client. Please run the Connect SSH Component");
            }

            if(sb != null)
            {
                sb.Append("[");
                sb.Append(DateTime.Now.ToShortDateString());
                sb.Append(" ");
                sb.Append(DateTime.Now.ToShortTimeString());
                sb.Append("] Uploaded ");
                sb.Append(SshPath);
                sb.Append("/");
                sb.Append(Path.GetFileName(localFileName));
                sb.Append("\n");
            }



        }

        public static bool IsSshConnected()
        {
            if (SSH_Helper.SshClient != null && SSH_Helper.SshClient.IsConnected)
            {


                return true;


            }
            else if (SSH_Helper.SshClient != null)
            {
                throw new Renci.SshNet.Common.SshConnectionException("SSH: There is a SSH client but no connection. Please run the Connect SSH Component");
            }
            else
            {
                throw new Renci.SshNet.Common.SshConnectionException("SSH: There is no SSH Client. Please run the Connect SSH Component");
            }
        }


        public static void Execute(string command, StringBuilder sb)
        {
            if (IsSshConnected())
            {
                sb.Append("\n[");
                sb.Append(DateTime.Now.ToShortDateString());
                sb.Append(" ");
                sb.Append(DateTime.Now.ToShortTimeString());
                sb.Append("] $ ");
                sb.Append(command);
                sb.Append("\n");
                var cmd = sshClient.CreateCommand(command);
                cmd.Execute();
                sb.AppendLine(cmd.Result);
            }

        }

        public static string Execute(string command)
        {
            if (IsSshConnected())
                return sshClient.CreateCommand(command).Execute();
            else return null;
        }



        public static void Disconnect()
        {
            sshClient?.Disconnect();
            sshClient?.Dispose();
            sshClient = null;

            sftpClient?.Disconnect();
            sftpClient?.Dispose();
            sftpClient = null;
        }
    }
}

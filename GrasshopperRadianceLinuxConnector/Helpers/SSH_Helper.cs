using System;
using System.Collections.Generic;
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

        private static SshClient sshClient;


        public static SftpClient SftpClient
        {
            get => sftpClient;
            set
            {
                if (sftpClient == null)
                    sftpClient = value;

                else
                {
                    sftpClient.Disconnect();
                    sftpClient.Dispose();
                    sftpClient = value;

                }

            }
        }

        private static SftpClient sftpClient;

        public static void Upload(string localFileName, string SshPath = "~/simulation")
        {

            if (SSH_Helper.SftpClient != null && !SSH_Helper.SftpClient.IsConnected)
            {

                SSH_Helper.SftpClient.ChangeDirectory(SshPath);

                if (!System.IO.File.Exists(localFileName))
                    throw new System.IO.FileNotFoundException(localFileName);


                using (var uplfileStream = System.IO.File.OpenRead(localFileName))
                {
                    SSH_Helper.SftpClient.UploadFile(uplfileStream, localFileName, true);
                }


            }
            else if (SSH_Helper.SftpClient != null)
            {
                throw new Renci.SshNet.Common.SshConnectionException("there is a Sftp client but no connection");
            }
            else
            {
                throw new NullReferenceException("no Sftp client");
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
                throw new Renci.SshNet.Common.SshConnectionException("there is a SSH client but no connection");
            }
            else
            {
                throw new NullReferenceException("There is no SSH Client");
            }
        }


        public static void Execute(string command, StringBuilder sb)
        {
            if (IsSshConnected())
                if ((bool)(sshClient?.IsConnected))
                {
                    sb.Append("\n\n [");
                    sb.Append(DateTime.Now.ToShortDateString());
                    sb.Append(" ");
                    sb.Append(DateTime.Now.ToShortTimeString());
                    sb.Append("] $ ");
                    sb.Append(command);
                    
                    sb.Append("\n\n");
                    sb.AppendLine(sshClient.CreateCommand(command).Execute());
                }
                else
                    throw new Exception("SSH not connected");
        }

        public static string Execute(string command)
        {
            if (IsSshConnected())
                if ((bool)(sshClient?.IsConnected))
                    return sshClient.CreateCommand(command).Execute();
                else
                    throw new Exception("SSH not connected");
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

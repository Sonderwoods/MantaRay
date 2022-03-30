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

        public static string ToLinuxPath(this string s)
        {
            if (s.StartsWith(WindowsParentPath))
            {
                return linuxParentPath + "/" + s.Substring(0, WindowsFullpath.Length).Replace(@"\", "/");
            }
            else
                return s.Replace(@"\", "/");
        }

        public static string ToWindowsPath(this string s)
        {
            if (s.StartsWith(LinuxParentPath))
            {
                return windowsParentPath + @"\" + s.Substring(0, LinuxParentPath.Length).Replace("/", @"\");
            }
            else
                return s.Replace("/", @"\");
        }

        public static string LinuxDir(string subfolderOverride = null)
        {
            if (!string.IsNullOrEmpty(subfolderOverride))
            {
                return linuxParentPath + "/" + subfolderOverride.Replace(@"\", "/");
            }
            else
                return _linuxFullpath;
        }

        public static string WindowsDir(string subfolderOverride = null)
        {
            if (!string.IsNullOrEmpty(subfolderOverride))
            {
                return windowsParentPath + @"\" + subfolderOverride.Replace("/", @"\");
            }
            else
                return _windowsFullpath;
        }


        /// <summary>
        /// Path without any ending slash
        /// </summary>
        public static string LinuxParentPath { get => linuxParentPath; set { { linuxParentPath = value; UpdatePaths(); } } }
        static string linuxParentPath = "~";


        /// <summary>
        /// Path without any ending backslash
        /// </summary>
        public static string WindowsParentPath { get => windowsParentPath; set { windowsParentPath = value; UpdatePaths(); } }
        static string windowsParentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>
        /// default subfolder WITHOUT starting slash.
        /// </summary>
        public static string DefaultSubfolder { get => defaultSubfolder; set { defaultSubfolder = value; UpdatePaths(); } }
        static string defaultSubfolder = "simulation";

        static string _linuxFullpath = linuxParentPath + "/" + defaultSubfolder.Replace(@"\", "/");
        static string _windowsFullpath = windowsParentPath + @"\" + defaultSubfolder.Replace("/", @"\");

        public static string LinuxFullpath => _linuxFullpath;
        public static string WindowsFullpath => _windowsFullpath;




        /// <summary>
        /// Will be set on connection
        /// </summary>
        public static string HomeDirectory { get; set; } = null;

        /// <summary>
        /// The suffixes to setup before any commands. Temporary fix untill we get .bashrc correctly setup.
        /// </summary>
        public static List<string> Suffixes { get; set; } = new List<string>() {
            "export PATH=$PATH:/usr/local/radiance/bin",
            "export RAYPATH=./usr/local/radiance/lib",
            "export DISPLAY=$(ip route list default | awk '{print $3}'):0",
            "export LIBGL_ALWAYS_INDIRECT=1"
        };

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


        public static void Upload(string localFileName, string sshPath = null, StringBuilder sb = null)
        {
            if (string.IsNullOrEmpty(sshPath))
            {
                sshPath = _linuxFullpath;
            }

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

            if (sb != null)
            {
                sb.Append("[");
                sb.Append(DateTime.Now.ToShortDateString());
                sb.Append(" ");
                sb.Append(DateTime.Now.ToShortTimeString());
                sb.Append("] Uploaded ");
                sb.Append(sshPath);
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




        public static void Execute(string command, StringBuilder log = null, StringBuilder stdout = null, StringBuilder errors = null, bool prependSuffix = true)
        {


            if (IsSshConnected())
            {
                if (log != null)
                {
                    log.Append("[");
                    log.Append(DateTime.Now.ToShortDateString());
                    log.Append(" ");
                    log.Append(DateTime.Now.ToShortTimeString());
                    log.Append("] $ ");
                    log.Append(command);
                }


                if (prependSuffix)
                    command = String.Join("\n", Suffixes) + ";" + command;

                var cmd = sshClient.CreateCommand(command);
                cmd.Execute();



                if (!string.IsNullOrEmpty(cmd.Error) && errors != null)
                {
                    errors.Append("[");
                    errors.Append(DateTime.Now.ToShortDateString());
                    errors.Append(" ");
                    errors.Append(DateTime.Now.ToShortTimeString());
                    errors.Append("] $ ");
                    errors.Append(command.Substring(0, Math.Min(30, command.Length - 1)));
                    errors.Append("\n");

                    errors.Append(cmd.Error);
                    errors.Append("\n");


                }


                //stdout.Append("\n");
                if (stdout != null)
                {
                    stdout.Append(cmd.Result.Trim('\n', '\r'));

                }


            }

        }



        public static string Execute(string command, out string error, bool prependSuffix = true)
        {
            if (IsSshConnected())
            {
                if (prependSuffix)
                    command += ";" + String.Join(";", Suffixes);
                //string randomFilename = Path.GetRandomFileName();
                var cmd = sshClient.CreateCommand($"{command}");
                cmd.Execute();

                error = cmd.Error;

                return cmd.Result.Trim('\n', '\r');

            }

            {
                error = "Not Connected";
                return null;

            }
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


        static void UpdatePaths()
        {

            windowsParentPath = windowsParentPath.Replace("/", @"\");

            if (windowsParentPath.EndsWith(@"\"))
            {
                windowsParentPath = windowsParentPath.Substring(0, windowsParentPath.Length - 1);
            }


            linuxParentPath = linuxParentPath.Replace(@"\", "/");

            if (linuxParentPath.EndsWith("/"))
            {
                linuxParentPath = linuxParentPath.Substring(0, linuxParentPath.Length - 1);
            }

            _linuxFullpath = linuxParentPath + "/" + defaultSubfolder.Replace(@"\", "/");
            _windowsFullpath = windowsParentPath + "/" + defaultSubfolder.Replace("/", @"\");

        }
    }
}

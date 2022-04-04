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
                return linuxParentPath + s.Substring(WindowsParentPath.Length).Replace(@"\", "/");
            }
            else
                return s.Replace(@"\", "/");
        }

        public static string ToWindowsPath(this string s)
        {
            if (s.StartsWith(LinuxParentPath))
            {
                return windowsParentPath + s.Substring(0, LinuxParentPath.Length).Replace("/", @"\");
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



        public static void Download(string linuxFileName, string localTargetFolder, StringBuilder status = null)
        {
            linuxFileName = linuxFileName.Replace("\\", "/");

            if (SSH_Helper.SftpClient != null && SSH_Helper.SftpClient.IsConnected)
            {
                //try
                //{
                    

                    if (string.IsNullOrEmpty(localTargetFolder))
                    {
                        localTargetFolder = _windowsFullpath;
                    }

                    localTargetFolder = localTargetFolder.TrimEnd('\\') + "\\";

                    //using (var remoteFileStream = SSH_Helper.SftpClient.OpenRead(linuxFileName))
                    //{
                    //    var textReader = new System.IO.StreamReader(remoteFileStream);
                    //    string s = textReader.ReadToEnd();
                    //    File.WriteAllText(localTargetFolder +Path.GetFileName(linuxFileName.Replace("/", "\\")), s);
                    //}



                    using (var saveFile = File.OpenWrite(localTargetFolder + Path.GetFileName(linuxFileName.Replace("/", "\\"))))
                    {
                        SSH_Helper.SftpClient.DownloadFile(linuxFileName, saveFile);
                    }
                    

                //}
                //catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                //{
                //    throw new Renci.SshNet.Common.SftpPathNotFoundException($"Linux Path not found\n{e.Message}.\nTry {HomeDirectory}\nThe current working directory is {sftpClient.WorkingDirectory}");
                //}

                //if (!File.Exists(linuxFileName))
                //    throw new FileNotFoundException("Local file not found: " + linuxFileName);


                //using (var uplfileStream = File.OpenRead(linuxFileName))
                //{
                //    try
                //    {
                //        SSH_Helper.SftpClient.UploadFile(uplfileStream, Path.GetFileName(linuxFileName), true);

                //    }
                //    catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                //    {
                //        throw new Renci.SshNet.Common.SftpPermissionDeniedException($"Tried accessing {localTargetFolder}\nLocal file is {linuxFileName}\n{e.Message}", e);
                //    }
                //}

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

            if (status != null)
            {
                status.Append("[");
                status.Append(DateTime.Now.ToString("G"));
                status.Append("] Downloaded ");
                status.Append(localTargetFolder);
                status.Append("/");
                status.Append(Path.GetFileName(linuxFileName));
                status.Append("\n");
            }



        }




        public static void Upload(string localFileName, string sshPath = null, StringBuilder status = null)
        {
            localFileName = localFileName.Replace("/", "\\");

            if (string.IsNullOrEmpty(sshPath))
            {
                sshPath = _linuxFullpath;
            }

            if (String.Compare(Path.GetDirectoryName(localFileName).ToLinuxPath(), sshPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                status?.Append("The paths are the same, so skipping the upload\n");
                return;
            }



            if (SSH_Helper.SftpClient != null && SSH_Helper.SftpClient.IsConnected)
            {
                try
                {
                    HomeDirectory = HomeDirectory ?? sftpClient.WorkingDirectory;

                    

                    sshPath = sshPath.TrimEnd('/');

                    sshPath = sshPath.Replace("~", HomeDirectory);

                    SSH_Helper.Execute($"mkdir -p {sshPath}");
                    //SSH_Helper.SftpClient.ChangeDirectory(SshPath);
                    SSH_Helper.SftpClient.ChangeDirectory(sshPath);
                }
                catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                {
                    throw new Renci.SshNet.Common.SftpPathNotFoundException($"Linux Path not found\n{e.Message}.\nTry {HomeDirectory}\nThe current working directory is {sftpClient.WorkingDirectory}");
                }

                if (!File.Exists(localFileName))
                    throw new FileNotFoundException("Local file not found: " + localFileName);


                using (var uplfileStream = File.OpenRead(localFileName))
                {
                    try
                    {
                    SSH_Helper.SftpClient.UploadFile(uplfileStream, Path.GetFileName(localFileName), true);

                    }
                    catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                    {
                        throw new Renci.SshNet.Common.SftpPermissionDeniedException($"Tried accessing {sshPath}\nLocal file is {localFileName}\n{e.Message}", e);
                    }
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

            if (status != null)
            {
                status.Append("[");
                status.Append(DateTime.Now.ToString("G"));
                status.Append("] Uploaded ");
                status.Append(sshPath);
                status.Append("/");
                status.Append(Path.GetFileName(localFileName));
                status.Append("\n");
            }



        }

        public static ConnectionDetails CheckConnection()
        {
            if (SSH_Helper.SshClient != null && SSH_Helper.SshClient.IsConnected)
            {


                return ConnectionDetails.Connected;


            }
            else if (SSH_Helper.SshClient != null)
            {
                return ConnectionDetails.ClientNoConnection;
            }
            else
            {
                return ConnectionDetails.NoClient;
            }
        }

        public enum ConnectionDetails
        {
            Connected,
            ClientNoConnection,
            NoClient
        }




        public static bool Execute(string command, StringBuilder log = null, StringBuilder stdout = null, StringBuilder errors = null, bool prependSuffix = true)
        {
            bool success = true;

            if (CheckConnection() == ConnectionDetails.Connected)
            {
                if (log != null)
                {
                    log.Append("[");
                    log.Append(DateTime.Now.ToString("G"));

                    log.Append("] $ ");
                    log.Append(command);
                    log.Append("\n");
                }


                //if (prependSuffix)
                //    command = String.Join("\n", Suffixes) + ";" + command;

                var cmd = sshClient.CreateCommand(prependSuffix ? String.Join("\n", Suffixes) + ";" + command : command);
                cmd.Execute();



                if (!string.IsNullOrEmpty(cmd.Error) && errors != null)
                {
                    errors.Append("[");
                    errors.Append(DateTime.Now.ToString("G"));
                    errors.Append("] stderrr $ ");
                    errors.Append(command.Substring(0, Math.Min(500, command.Length)).Replace("\n","\n    ").Replace(";","\n    "));
                    errors.Append("\n");

                    errors.Append(cmd.Error);
                    errors.Append("\n");

                    success = false;


                }


                //stdout.Append("\n");
                if (stdout != null)
                {
                    stdout.Append(cmd.Result.Trim('\n', '\r'));

                }


            }
            else
            {
                if (log != null)
                {
                    log.Append("[");
                    log.Append(DateTime.Now.ToString("G"));

                    log.Append("] $ ");
                    log.Append(command);
                    log.Append("\n ERROR: There was no connection. Please run the connect component again");
                }

                if (errors != null)
                {
                    errors.Append("[");
                    errors.Append(DateTime.Now.ToString("G"));

                    errors.Append("] $ ");
                    errors.Append(command);
                    errors.Append("\n ERROR: There was no connection. Please run the connect component again");
                }
                return false;
            }

            return success;

        }



        public static string Execute(string command, out string error, bool prependSuffix = true)
        {
            if (CheckConnection() == ConnectionDetails.Connected)
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

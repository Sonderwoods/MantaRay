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

        private static Random rnd = new Random();

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
                    sftpClient.BufferSize = 4096;

                }

            }
        }

        private static SftpClient sftpClient;

        public static bool FileExistsInLinux(string path)
        {
            StringBuilder sb = new StringBuilder(1);
            Execute($"[ -f {path} ] && echo \"1\" || echo \"0\"", stdout: sb);
            return string.Equals("1", sb.ToString());
        }

        public static bool FileExistsInWindows(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Can read a file no matter if you input a linux or a windows path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static string ReadFile(string path)
        {
            if (FileExistsInWindows(path))
            {
                return File.ReadAllText(path);
            }
            else if (FileExistsInLinux(path))
            {
                StringBuilder sb = new StringBuilder();
                Execute($"cat \"path\"", stdout: sb);
                return sb.ToString();
            }
            else
            {
                throw new FileNotFoundException("could not find the file on windows or linux", path);
            }
        }



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




        public static int Execute(string command, StringBuilder log = null, StringBuilder stdout = null, StringBuilder errors = null, bool prependSuffix = true)
        {
            if (string.IsNullOrEmpty(command))
            {
                errors?.Append("Command was empty");
                return -1;

            }
            //bool success = false;
            int pid = -1;

            if (CheckConnection() == ConnectionDetails.Connected)
            {
                DateTime startTime = DateTime.Now;

                if (TryRunXlaunchIfNeeded(command) && log != null)
                {
                    log.Append("* (attempted) Started XLaunch because you forgot to do so * ");
                }

                //if (prependSuffix)
                //    command = String.Join("\n", Suffixes) + ";" + command;
                int rand = rnd.Next(10000, 99999);

                command = command.Trim('\n');



                var cmd = sshClient.CreateCommand((prependSuffix ? String.Join(";", Suffixes) + ";" + command : command) + $" & echo $! >~/temp{rand}.pid");
                cmd.Execute();

                var pidCommand = sshClient.CreateCommand($"cat  ~/temp{rand}.pid");
                pidCommand.Execute();


                if (log != null)
                {
                    log.Append("[");
                    log.Append(startTime.ToString("G"));

                    log.Append("] (");
                    log.Append((DateTime.Now - startTime).TotalMilliseconds);
                    log.Append("ms ) $ ");
                    log.Append(command.Replace("\n", "\n   ").Replace(";", "\n   "));
                    log.Append("\n");
                }



                if (string.IsNullOrEmpty(cmd.Error))
                {
                    //success = true;
                    pid = int.Parse(pidCommand.Result);
                }
                else
                {
                    if (errors != null)
                    {
                        //errors.Append("[");
                        //errors.Append(DateTime.Now.ToString("G"));
                        //errors.Append("] stderrr $ ");
                        //errors.Append(command.Substring(0, Math.Min(500, command.Length)).Replace("\n", "\n    ").Replace(";", "\n    "));
                        //errors.Append("\n");

                        errors.Append(cmd.Error);
                        errors.Append("\n");

                    }




                }


                //stdout.Append("\n");
                if (stdout != null)
                {
                    stdout.Append(cmd.Result.Trim('\n', '\r'));

                }


            }
            else // no connection
            {
                if (log != null)
                {
                    log.Append("[");
                    log.Append(DateTime.Now.ToString("G"));

                    log.Append("] $ ");
                    log.Append(command.Replace("\n", "\n   ").Replace(";", "\n   "));
                    log.Append("\n ERROR: There was no connection. Please run the connect component again");
                }

                if (errors != null)
                {
                    errors.Append("[");
                    errors.Append(DateTime.Now.ToString("G"));

                    errors.Append("] $ ");
                    errors.Append(command.Replace("\n", "\n   ").Replace(";", "\n   "));
                    errors.Append("\n ERROR: There was no connection. Please run the connect component again");
                }

            }

            return pid;

        }

        private static bool TryRunXlaunchIfNeeded(string command)
        {
            var j = System.Diagnostics.Process.GetProcesses().Where(p => p.ProcessName.ToLower() == "vcxsrv");
            bool hasXlaunchRunnning = System.Diagnostics.Process.GetProcesses().Any(p => p.ProcessName.ToLower() == "vcxsrv");
            //bool hasXlaunchRunnning = System.Diagnostics.Process.GetProcessesByName("vcxsrv.exe").Any();

            if (hasXlaunchRunnning)
                return false;


            string[] xcommands = { "x11meta", "xglaresrc", "ximage", "xshowtrace", "xclock" };

            bool run = false;

            foreach (var xcom in xcommands)
            {
                if (command.Contains(xcom))
                {
                    run = true;
                    continue;
                }
            }

            if (!run)
                return false;

            return true;

            //TODO: Need to figure out way to add auth to xlaunch files. Skipping for now.


            string xlauncFile = @"<?xml version='1.0' encoding='UTF-8'?>
<XLaunch WindowMode='MultiWindow' ClientMode='NoClient' LocalClient='False' Display='-1' LocalProgram='xcalc' RemoteProgram='xterm' RemotePassword='' PrivateKey='' RemoteHost='' RemoteUser='' XDMCPHost='' XDMCPBroadcast='False' XDMCPIndirect='False' Clipboard='True' ClipboardPrimary='True' ExtraParams='' Wgl='True' DisableAC='True' XDMCPTerminate='False'/>
";

            string tempFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".xlaunch";

            File.WriteAllText(tempFile, xlauncFile);

            System.Diagnostics.Process.Start(tempFile);

            File.Delete(tempFile);

            return true;
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

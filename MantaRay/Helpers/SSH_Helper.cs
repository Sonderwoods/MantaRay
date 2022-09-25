using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;


namespace MantaRay
{
    internal static class SSH_Helper
    {
        /// <summary>
        /// Path without any ending slash
        /// </summary>
        public static string LinuxParentPath { get => linuxParentPath; set { { linuxParentPath = value; UpdatePaths(); } } }

        /// <summary>
        /// Path without any ending backslash
        /// </summary>
        public static string WindowsParentPath { get => windowsParentPath; set { windowsParentPath = value; UpdatePaths(); } }

        /// <summary>
        /// Linux full path for the default folder WITHOUT ending slash
        /// </summary>
        public static string LinuxFullpath => _linuxFullpath;

        /// <summary>
        /// Windows full path for the default folder WITHOUT ending slash
        /// </summary>
        public static string WindowsFullpath => _windowsFullpath;

        public static string SftpDir { get; set; } = "";

        /// <summary>
        /// default subfolder WITHOUT starting slash.
        /// </summary>
        public static string DefaultSubfolder { get => projectSubFolder; set { projectSubFolder = value; UpdatePaths(); } }

        /// <summary>
        /// Will be set on connection
        /// </summary>
        public static string HomeDirectory { get; set; } = null;


        /// <summary>
        /// The suffixes to setup before any commands. Temporary fix untill we get .bashrc correctly setup.
        /// </summary>
        public static string ExportPrefixes { get; set; } =
            "export PATH=$PATH:/usr/local/radiance/bin;" +
            "export RAYPATH=.:/usr/local/radiance/lib;" + //including local dir
            "export DISPLAY=$(ip route list default | awk '{print $3}'):0;" +
            "export LIBGL_ALWAYS_INDIRECT=1";


        public static string ExportPrefixesDefault { get; } =
            "export PATH=$PATH:/usr/local/radiance/bin;" +
            "export RAYPATH=.:/usr/local/radiance/lib;" + //including local dir
            "export DISPLAY=$(ip route list default | awk '{print $3}'):0;" +
            "export LIBGL_ALWAYS_INDIRECT=1";



        public static SshClient SshClient
        {
            get => sshClient;
            set
            {
                if (sshClient == null)
                {
                    sshClient = value;
                    HomeDirectory = null;

                }

                else
                {
                    sshClient.Disconnect();
                    sshClient.Dispose();
                    sshClient = value;
                    HomeDirectory = null;

                }

            }
        }

        private static readonly Random rnd = new Random();

        public static string ToLinuxPath(this string s)
        {

            if (sftpClient == null)
                throw new System.NullReferenceException("No Connection");

            if (s == null)
                return null;

            HomeDirectory = HomeDirectory ?? sftpClient.WorkingDirectory;

            if (s.StartsWith(WindowsParentPath))
            {
                return (linuxParentPath + s.Substring(WindowsParentPath.Length)).Replace(@"\", "/").Replace("~", HomeDirectory);
            }
            else
                return s.Replace(@"\", "/");
        }

        public static string ToWindowsPath(this string s)
        {
            if (sftpClient == null)
                throw new System.NullReferenceException("No Connection");

            if (s == null)
                return null;

            if (s.StartsWith(LinuxParentPath))
            {
                return (windowsParentPath + s.Substring(LinuxParentPath.Length)).Replace("/", @"\");
            }
            else if (s.StartsWith(HomeDirectory))
            {
                return (windowsParentPath + s.Substring(HomeDirectory.Length)).Replace("/", @"\");
            }
            else
                return s.Replace("/", @"\");
        }

        public static string LinuxDir(string subfolderOverride = null)
        {
            if (!string.IsNullOrEmpty(subfolderOverride))
            {
                return (linuxParentPath + "/" + subfolderOverride).Replace(@"\", "/");
            }
            else
                return _linuxFullpath;
        }

        public static string WindowsDir(string subfolderOverride = null)
        {
            if (!string.IsNullOrEmpty(subfolderOverride))
            {
                return (windowsParentPath + @"\" + subfolderOverride).Replace("/", @"\");
            }
            else
                return _windowsFullpath;
        }

        static string projectSubFolder = String.Empty;
        static public string DefaultProjectSubFolder => "UnnamedProject";

        static string linuxParentPath = String.Empty;
        static public string DefaultLinuxParentPath => "~/MantaRay";

        static string windowsParentPath = String.Empty;
        static public string DefaultWindowsParentPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\MantaRay";

        static string _linuxFullpath = (linuxParentPath + "/" + projectSubFolder).Replace(@"\", "/");

        static string _windowsFullpath = (windowsParentPath + @"\" + projectSubFolder).Replace("/", @"\");

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



        public static void Download(string linuxFileName, string localTargetFolder, StringBuilder log = null)
        {
            linuxFileName = linuxFileName.Replace("\\", "/");



            if (SSH_Helper.SftpClient != null && SSH_Helper.SftpClient.IsConnected)
            {

                if (string.IsNullOrEmpty(localTargetFolder))
                {
                    localTargetFolder = _windowsFullpath;
                }

                localTargetFolder = localTargetFolder.TrimEnd('\\') + "\\";
                string targetFileName = localTargetFolder + Path.GetFileName(linuxFileName.Replace("/", "\\"));






                using (var saveFile = File.OpenWrite(targetFileName))
                {
                    try
                    {
                        SSH_Helper.SftpClient.DownloadFile(linuxFileName, saveFile);

                    }
                    catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                    {
                        if (String.Compare(targetFileName.ToLinuxPath(), linuxFileName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            log?.Append("The paths are the same, so skipping the download\n");
                            return;
                        }
                        else
                        {
                            throw e;
                        }

                    }

                }


            }
            else if (SSH_Helper.SftpClient != null)
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is a Sftp client but no connection. Please run the Connect SSH Component");
            }
            else
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is no Sftp client. Please run the Connect SSH Component");
            }

            if (log != null)
            {
                log.Append("[");
                log.Append(DateTime.Now.ToString("G"));
                log.Append("] Downloaded ");
                log.Append(localTargetFolder);
                log.Append(Path.GetFileName(linuxFileName));
                log.Append("\n");
            }

        }

        /// <summary>
        /// Uploads a file
        /// </summary>
        /// <param name="localFileName"></param>
        /// <param name="targetFilePath">target linux path. Default is <see cref="projectSubFolder"/></param>
        /// <param name="log"></param>
        /// <exception cref="Renci.SshNet.Common.SftpPathNotFoundException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Renci.SshNet.Common.SftpPermissionDeniedException"></exception>
        /// <exception cref="Renci.SshNet.Common.SshConnectionException"></exception>
        public static void Upload(string localFileName, string targetFilePath = null, StringBuilder log = null)
        {
            //localFileName = localFileName.Replace("/", "\\");

            //if (string.IsNullOrEmpty(targetFilePath))
            //{
            //    targetFilePath = _linuxFullpath;
            //}


            //if (String.Compare(Path.GetDirectoryName(localFileName).ToLinuxPath(), sshPath, StringComparison.OrdinalIgnoreCase) == 0)
            //{
            //    log?.Append("The paths are the same, so skipping the upload\n");
            //    return;
            //}


            if (SSH_Helper.SftpClient == null)
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is no Sftp client. Please run the Connect SSH Component");
            }
            else if (!SSH_Helper.SftpClient.IsConnected)
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is a Sftp client but no connection. Please run the Connect SSH Component");
            }


            //try
            //{
            //    HomeDirectory = HomeDirectory ?? sftpClient.WorkingDirectory; // Set it once.

            //    //targetFilePath = targetFilePath.TrimEnd('/');

            if (targetFilePath.Contains("~"))
            {
                StringBuilder sb = new StringBuilder();
                SSH_Helper.Execute($"readlink -f {targetFilePath}", null, sb, null, false, false, null);
                targetFilePath = sb.ToString();
            }

            //    //SSH_Helper.Execute($"mkdir -p {targetFilePath}");

            //    var v = SSH_Helper.SftpClient.WorkingDirectory;

            //    //if (!string.IsNullOrEmpty(targetFilePath))
            //    //    SSH_Helper.SftpClient.ChangeDirectory(targetFilePath.Contains(":") ? "/" : "" + targetFilePath);
            //    //else
            //    //    throw new Renci.SshNet.Common.SftpPathNotFoundException("Path is not set");
            //}

            //catch (Renci.SshNet.Common.SftpPathNotFoundException e)
            //{
            //    throw new Renci.SshNet.Common.SftpPathNotFoundException($"Linux Path not found\n{e.Message}.\nTry {HomeDirectory} (linux HomeDirectory)\nTried to set the working directory to {targetFilePath}\n(It was set to {sftpClient.WorkingDirectory})");
            //}


            if (!File.Exists(localFileName))
            {
                throw new FileNotFoundException("Local file not found: " + localFileName);
            }


            using (var uplfileStream = File.OpenRead(localFileName))
            {
                try
                {
                    SSH_Helper.SftpClient.UploadFile(uplfileStream, targetFilePath + (String.IsNullOrEmpty(targetFilePath) ? "" : "/") + Path.GetFileName(localFileName), true);

                }
                catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                {


                    if (String.Compare(Path.GetDirectoryName(localFileName).ToLinuxPath(), targetFilePath, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        log?.Append("The paths are the same, so skipping the download\n");
                        return;
                    }
                    else
                    {
                        throw new Renci.SshNet.Common.SftpPermissionDeniedException($"Tried accessing {targetFilePath}\nLocal file is {localFileName}\n{e.Message}", e);
                    }



                }
            }


            if (log != null)
            {
                log.Append("[");
                log.Append(DateTime.Now.ToString("G"));
                log.Append("] Uploaded ");
                log.Append(targetFilePath);
                log.Append("/");
                log.Append(Path.GetFileName(localFileName));
                log.Append("\n");
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


        /// <summary>
        /// Executes the SSH Command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="log">stringbuilder for logs</param>
        /// <param name="stdout">stringbuilder for stdout</param>
        /// <param name="errors">stringbuilder for errors</param>
        /// <param name="prependPrefix">whether we want to include the radiance "EXPORT" prefixes <see cref="ExportPrefixes"/></param>
        /// <returns></returns>
        public static (IAsyncResult, SshCommand, int) ExecuteAsync(string command, bool prependPrefix = true, bool appendSuffix = true, Func<string, bool> filter = null)
        {
            IAsyncResult asyncResult = null;



            int pid = -1;

            if (CheckConnection() == ConnectionDetails.Connected)
            {
                DateTime startTime = DateTime.Now;

                TryRunXlaunchIfNeeded(command);



                command = command.Trim('\n');

                int rand = rnd.Next(100000, 999999);

                //saving the pid to a local file
                SshCommand cmd = sshClient.CreateCommand((prependPrefix ? ExportPrefixes + ";" + command : command) + (appendSuffix ? $" & echo $! >~/temp{rand}.pid" : ""));

                asyncResult = cmd.BeginExecute();

                pid = GetPid(appendSuffix, rand);

                return (asyncResult, cmd, pid);






            }


            return (asyncResult, null, pid);

        }

        public static void OnExecutionCompleted(SshCommand cmd, Func<string, bool> filter = null, StringBuilder stdout = null, StringBuilder errors = null)
        {

            if (!string.IsNullOrEmpty(cmd.Error) && (filter == null || filter(cmd.Error)))
            {
                errors?.Append(cmd.Error);
                errors?.Append("\n");
            }

            stdout?.Append(cmd.Result.Trim('\n', '\r'));

            // Here we have to add a way to recompute the component

        }

        private static int GetPid(bool appendSuffix, int rand)
        {
            if (appendSuffix)
            {


                var pidCmd = sshClient.CreateCommand($"cat  ~/temp{rand}.pid");
                pidCmd.Execute();


                int pid = -1;
                int.TryParse(pidCmd.Result, out pid);

                sshClient.CreateCommand($"rm ~/temp{rand}.pid").Execute(); // remove temp file again

                return pid;

            }
            else
            {
                return -1;
            }
        }




        /// <summary>
        /// Executes the SSH Command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="log">stringbuilder for logs</param>
        /// <param name="stdout">stringbuilder for stdout</param>
        /// <param name="errors">stringbuilder for errors</param>
        /// <param name="prependPrefix">whether we want to include the radiance "EXPORT" prefixes <see cref="ExportPrefixes"/></param>
        /// <returns></returns>
        public static int Execute(string command, StringBuilder log = null, StringBuilder stdout = null, StringBuilder errors = null, bool prependPrefix = true, bool appendSuffix = true, Func<string, bool> filter = null)
        {
            if (string.IsNullOrEmpty(command))
            {
                errors?.Append("Command was empty");
                return -1;

            }

            int pid = -1;

            if (CheckConnection() == ConnectionDetails.Connected)
            {
                DateTime startTime = DateTime.Now;

                if (TryRunXlaunchIfNeeded(command) && log != null)
                {
                    log.Append("* (attempted) Started XLaunch because you forgot to do so * ");
                }

                int rand = rnd.Next(10000, 99999);

                command = command.Trim('\n');

                //saving the pid to a local file
                var cmd = sshClient.CreateCommand((prependPrefix ? ExportPrefixes + ";" + command : command) + (appendSuffix ? $" & echo $! >~/temp{rand}.pid" : ""));
                cmd.Execute();

                if (appendSuffix)
                {
                    var pidCommand = sshClient.CreateCommand($"cat  ~/temp{rand}.pid");

                    pidCommand.Execute();

                    if (string.IsNullOrEmpty(cmd.Error))
                    {
                        if (!int.TryParse(pidCommand.Result, out pid))
                            pid = -1;

                    }

                    sshClient.CreateCommand($"rm ~/temp{rand}.pid").Execute();
                }
                else
                {
                    pid = string.IsNullOrEmpty(cmd.Error) ? 1 : -1;
                }



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

                bool ok = filter != null ? filter(cmd.Error) : true;

                if (!string.IsNullOrEmpty(cmd.Error) && (filter == null || filter(cmd.Error)))
                {
                    errors?.Append(cmd.Error);
                    errors?.Append("\n");
                }

                stdout?.Append(cmd.Result.Trim('\n', '\r'));



            }
            else // no connection
            {
                if (log != null)
                {
                    log.Append("[");
                    log.Append(DateTime.Now.ToString("G"));
                    log.Append("] $ ");
                    log.Append(String.Join("\n", command.Replace("\n", "\n   ").Replace(";", "\n   ").Split('\n').Take(5)));
                    log.Append("\n...\n ERROR: There was no connection. Please run the connect component again");
                }

                if (errors != null)
                {
                    errors.Append("[");
                    errors.Append(DateTime.Now.ToString("G"));
                    errors.Append("] $ ");
                    errors.Append(String.Join("\n", command.Replace("\n", "\n   ").Replace(";", "\n   ").Split('\n').Take(5)));
                    errors.Append("\n...\n ERROR: There was no connection. Please run the connect component again");
                }

            }

            return pid;

        }


        private static bool TryRunXlaunchIfNeeded(string command)
        {

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

            string xlauncFile = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                    "<XLaunch WindowMode='MultiWindow' ClientMode='NoClient' LocalClient='False' Display='-1' " +
                    "LocalProgram='xcalc' RemoteProgram='xterm' RemotePassword='' PrivateKey='' RemoteHost='' " +
                    "RemoteUser='' XDMCPHost='' XDMCPBroadcast='False' XDMCPIndirect='False' Clipboard='True' " +
                    "ClipboardPrimary='True' ExtraParams='-ac' Wgl='True' DisableAC='False' XDMCPTerminate='False'/>\n".Replace("'", "\"");

            string tempFile = Path.GetTempPath() + "config.xlaunch";

            File.WriteAllText(tempFile, xlauncFile);

            System.Diagnostics.Process.Start(tempFile);

            _ = Task.Run(() => DelayedFileDelete(tempFile));

            return true;
        }

        public static void DelayedFileDelete(string tempFile, int delay = 3000)
        {
            Thread.Sleep(delay);

            File.Delete(tempFile);
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

            _linuxFullpath = linuxParentPath + "/" + projectSubFolder.Replace(@"\", "/");
            _windowsFullpath = (windowsParentPath + "/" + projectSubFolder).Replace("/", @"\");

        }

        public enum ConnectionDetails
        {
            Connected,
            ClientNoConnection,
            NoClient
        }
    }
}

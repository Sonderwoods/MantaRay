using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Renci.SshNet;
using Rhino.UI;
using static MantaRay.Helpers.PathHelper;


namespace MantaRay
{
    public class SSH_Helper : IFolderConversion
    {

        /// <summary>
        /// Path without any ending slash
        /// </summary>
        public string LinuxParentPath { get => linuxParentPath; set { linuxParentPath = value; UpdatePaths(); } }

        /// <summary>
        /// Path without any ending backslash
        /// </summary>
        public string WindowsParentPath { get => windowsParentPath; set { windowsParentPath = value; UpdatePaths(); } }

        /// <summary>
        /// Linux full path for the default folder WITHOUT ending slash
        /// </summary>
        public string LinuxHome => _linuxFullpath;

        /// <summary>
        /// Windows full path for the default folder WITHOUT ending slash
        /// </summary>
        public string WinHome => _windowsFullpath;



        /// <summary>
        /// default subfolder WITHOUT starting slash.
        /// </summary>
        public string ProjectSubPath { get => projectSubFolder; set { projectSubFolder = value; UpdatePaths(); } }



        /// <summary>
        /// Will be set on connection
        /// </summary>
        public string HomeDirectory { get; set; } = null;


        /// <summary>
        /// Will be set on connection
        /// </summary>
        public string SftpHome { get; set; } = null;

        public string LinuxHomeReplacement { get => _linuxHomeReplacement ?? Execute($"readlink -f ~"); set => _linuxHomeReplacement = value; }

        string _linuxHomeReplacement;


        /// <summary>
        /// The suffixes to setup before any commands. Temporary fix untill we get .bashrc correctly setup.
        /// </summary>
        public string ExportPrefixes { get; set; } =
            "export PATH=$PATH:/usr/local/radiance/bin:/usr/local/accelerad/bin;" +
            "export RAYPATH=.:/usr/local/radiance/lib:/usr/local/accelerad/lib;" + //including local dir
            "export DISPLAY=$(ip route list default | awk '{print $3}'):0;" +
            "export LD_lIBRARY_PATH=/uusr/local/accelerad/bin:$LD_LIBRARY_PATH;" +
            "export LIBGL_ALWAYS_INDIRECT=1";


        public string ExportPrefixesDefault { get; } =
            "export PATH=$PATH:/usr/local/radiance/bin:/usr/local/accelerad/bin;" +
            "export RAYPATH=.:/usr/local/radiance/lib:/usr/local/accelerad/lib;" + //including local dir
            "export DISPLAY=$(ip route list default | awk '{print $3}'):0;" +
            "export LD_lIBRARY_PATH=/uusr/local/accelerad/bin:$LD_LIBRARY_PATH;" +
            "export LIBGL_ALWAYS_INDIRECT=1";



        public SshClient SshClient
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








        static string projectSubFolder = String.Empty;
        static public string DefaultProjectSubFolder => "UnnamedProject";

        static string linuxParentPath = String.Empty;
        static public string DefaultLinuxParentPath => "~/MantaRay";

        static string windowsParentPath = String.Empty;
        static public string DefaultWindowsParentPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\MantaRay";

        static string _linuxFullpath = (linuxParentPath + "/" + projectSubFolder).Replace(@"\", "/");

        static string _windowsFullpath = (windowsParentPath + @"\" + projectSubFolder).Replace("/", @"\");

        private SshClient sshClient;


        public SftpClient SftpClient
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

        public bool FileExistsInLinux(string path)
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
        public string ReadFile(string path)
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



        public void Download(string serverFilePath, string localTargetFolder, StringBuilder log = null)
        {
            //serverFilePath = serverFilePath.Replace("\\", "/");
            //if (SSH_Helper.sftpClient.WorkingDirectory.Contains(":"))
            //{
            //    serverFilePath = serverFilePath.ToSftpPath();
            //}
            serverFilePath = serverFilePath.Trim('\n', '\r').ToSftpPath();


            if (SftpClient != null && SftpClient.IsConnected)
            {

                if (string.IsNullOrEmpty(localTargetFolder))
                {
                    localTargetFolder = _windowsFullpath;
                }

                localTargetFolder = localTargetFolder.TrimEnd('\\') + "\\";
                string targetFileName = localTargetFolder + Path.GetFileName(serverFilePath.Trim('\n', '\r').Replace("/", "\\"));



                if (!Directory.Exists(Path.GetDirectoryName(targetFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFileName));
                }

                if (serverFilePath.Contains("~"))
                {
                    StringBuilder sb = new StringBuilder();
                    Execute($"readlink -f {serverFilePath}", null, sb, null, false, false, null);
                    serverFilePath = sb.Length > 0 ? sb.ToString() : serverFilePath;
                }


                using (var saveFile = File.OpenWrite(targetFileName))
                {
                    try
                    {
                        SftpClient.DownloadFile(serverFilePath, saveFile);

                    }
                    catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                    {
                        if (String.Compare(targetFileName.ToLinuxPath(), serverFilePath, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            log?.Append("The paths are the same, so skipping the download\n");
                            return;
                        }
                        else
                        {
                            throw e;
                        }

                    }
                    catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                    {
                        throw new FileNotFoundException($"Could not find {serverFilePath} on host"); 
                    }

                }


            }
            else if (SftpClient != null)
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
                log.Append(Path.GetFileName(serverFilePath));
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
        public string Upload(string localFileName, string targetFilePath = null, StringBuilder log = null)
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


            if (SftpClient == null)
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is no Sftp client. Please run the Connect SSH Component");
            }
            else if (!SftpClient.IsConnected)
            {
                throw new Renci.SshNet.Common.SshConnectionException("Sftp: There is a Sftp client but no connection. Please run the Connect SSH Component");
            }


            //try
            //{
            //    HomeDirectory = HomeDirectory ?? sftpClient.WorkingDirectory; // Set it once.

            //    //targetFilePath = targetFilePath.TrimEnd('/');

            if (targetFilePath != null && targetFilePath.Contains("~"))
            {
                targetFilePath = Execute($"readlink -f {targetFilePath}");

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

            string path = targetFilePath + (String.IsNullOrEmpty(targetFilePath) ? "" : "/") + Path.GetFileName(localFileName);

            string suffix = "";

            using (var uplfileStream = File.OpenRead(localFileName))
            {
                try
                {


                    //StringBuilder ss = new StringBuilder();

                    //Execute($"touch {path}", errors: ss);

                    //string v = SSH_Helper.SftpClient.WorkingDirectory;

                    if (SftpClient.WorkingDirectory.Contains(":") && path.Contains(":"))
                    {
                        path = path.ToSftpPath();
                    }
                    else
                    {
                        Execute($"mkdir -p {targetFilePath}");
                    }

                    try
                    {
                        if (!String.IsNullOrEmpty(targetFilePath))
                        {
                            SftpClient.ChangeDirectory(targetFilePath);

                        }

                    }
                    catch (Renci.SshNet.Common.SftpPathNotFoundException)
                    {
                        if (SftpClient.WorkingDirectory.Contains(":"))
                        {
                            //log?.Append("Could not find path \"");
                            //log?.Append(targetFilePath);
                            //log?.Append("\" so we keep the default path of \"");
                            //log?.Append(SSH_Helper.SftpClient.WorkingDirectory);
                            //log?.Append("\". This is most likely because you inputted a linux path and the SFTP is running with windows paths");
                            suffix = "-  WARNING: Paths are changed. Most likely because you inputted a linux path and your SFTP is running on Windows.\n";

                        }
                        //throw new Renci.SshNet.Common.SftpPathNotFoundException(

                        //    (SSH_Helper.SftpClient.WorkingDirectory.Contains(":") ?
                        //    "This 'could' be because the sftp connection is using windows paths.\n" : "") +
                        //    $"Your current path is {SSH_Helper.SftpClient.WorkingDirectory}\nInner Exception: " +
                        //    e.Message);
                    }
                    catch (Renci.SshNet.Common.SshException)
                    {
                        //if (e.Message == "Bad message" && SSH_Helper.SftpClient.WorkingDirectory.Contains(":") && !targetFilePath.StartsWith("/"))
                        //{
                        //    throw new Renci.SshNet.Common.SftpPathNotFoundException(
                        //        "Looks like you tried to change directory and are working with a windows Sftp file system.\n" +
                        //        "Try adding '/' in front of the path such as '/C:/Users...' even though it's not logical.\n" +
                        //        $"Default path is {SftpPath}");
                        //}
                    }

                    //int x = 0;

                    SftpClient.UploadFile(uplfileStream, Path.GetFileName(localFileName), true);
                }
                catch (Renci.SshNet.Common.SftpPermissionDeniedException e)
                {


                    if (String.Compare(Path.GetDirectoryName(localFileName).ToLinuxPath(), targetFilePath, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        log?.Append("The paths are the same, so skipping the download\n");
                        return path;
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
                log.Append("] Uploading ");
                log.Append(Path.GetFileName(localFileName));
                log.Append("\n- From: ");
                log.Append(Path.GetDirectoryName(localFileName));
                log.Append("\n- To: ");
                log.Append(SftpClient.WorkingDirectory);
                log.Append("\n");
                log.Append(suffix);
            }


            if (SftpClient.WorkingDirectory.Contains(":"))
            {
                return (SftpClient.WorkingDirectory + "/" + Path.GetFileName(localFileName)).ToWindowsPath();
            }
            else
            {
                return (SftpClient.WorkingDirectory + "/" + Path.GetFileName(localFileName)).ToLinuxPath();
            }

            //return SSH_Helper.SftpClient.WorkingDirectory + (SSH_Helper.SftpClient.WorkingDirectory.Contains(":") ? "\\" : "/") + Path.GetFileName(localFileName);

        }

        public ConnectionDetails CheckConnection()
        {
            if (SshClient != null && SshClient.IsConnected)
            {
                return ConnectionDetails.Connected;
            }
            else if (SshClient != null)
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
        public (IAsyncResult, SshCommand, int) ExecuteAsync(string command, bool prependPrefix = true, bool appendSuffix = true, Func<string, bool> filter = null)
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

        private int GetPid(bool appendSuffix, int rand)
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

        public string Execute(string command)
        {
            return sshClient.CreateCommand(command.ApplyGlobals(maxDepth: 2)).Execute().Trim();
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
        public int Execute(string command, StringBuilder log = null, StringBuilder stdout = null, StringBuilder errors = null, bool prependPrefix = true, bool appendSuffix = true, Func<string, bool> filter = null)
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

        public void Disconnect()
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


        /// <summary>
        /// Gets the active SSH Helper from the document
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static SSH_Helper CurrentFromDocument(GH_Document doc)
        {
            return doc.Objects.OfType<GH_Component>().Where(c => !c.Locked).OfType<ISetConnection>().FirstOrDefault().SshHelper;
        }

        public static SSH_Helper CurrentFromActiveDoc()
        {
            return Grasshopper.Instances.ActiveCanvas.Document.Objects.OfType<GH_Component>().Where(c => !c.Locked).OfType<ISetConnection>().FirstOrDefault().SshHelper;
        }

        public enum ConnectionDetails
        {
            Connected,
            ClientNoConnection,
            NoClient
        }
    }
}

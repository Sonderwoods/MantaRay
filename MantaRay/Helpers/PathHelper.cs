using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MantaRay.SSH_Helper;

namespace MantaRay.Helpers
{
    /// <summary>
    /// A helper class to convert between linux and windows paths.. and in some cases sftp paths.
    /// We are assuming three different file systems:
    /// <para>1) Your local windows environment. Here you have a local folder with your files.</para>
    /// 2) The Linux path to use in SSH (can be on a server or your local WSL)
    /// <para>3) In some cases with WSL running on a remote host, windows OpenSSH will offer SFTP with windows paths. These are however wrapped with forward slashes and a starting slash</para>
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// The path types available
        /// </summary>
        public enum PathType
        {
            Unknown,
            Linux,
            Windows,
            /// <summary>
            /// Sftp is here because SSHNET uses a weird path annotation such as a unixlooking windows path.
            /// <para>Example: /C:/Users/..</para>
            /// </summary>
            Sftp
        }

        /// <summary>
        /// Guess the path type based on the path. It looks for "/" vs "\" but also sees if the \<LinuxHome\> etc is involved.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static PathType GuessType(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return PathType.Unknown;
            }
            string lower = path.ToLower();

            // C: etc
            if (lower[1] == ':' && lower[0] >= 'a' && lower[0] <= 'z')
            {
                return PathType.Windows;
            }


            // First we check for Linux
            
            string[] lookups = new string[]
            {
                "~",
                "/mnt/",
                "/home/",
                "/user/",
                "/root/",
            };

            foreach (var lookup in lookups)
            {
                if (lower.Contains(lookup))
                    return PathType.Linux;
            }

            // Starts with "/"  --> either linux or SFTP
            if (path.StartsWith("/"))
            {
                Regex regexAdvanced = new Regex(@"([a-zA-Z]):\\(.*)", RegexOptions.Compiled);

                return regexAdvanced.IsMatch(lower) ? PathType.Sftp : PathType.Linux;

            }

            if (lower.ToCharArray().Count(a => a == '/') > lower.ToCharArray().Count(a => a == '\\'))
            {
                return PathType.Linux;
            }
            else
            {
                return PathType.Windows;
            }


        }

        /// <summary>
        /// Replaces /mnt/X to X:, changes to forward slash and replaces <see cref="SSH_Helper.LinuxHome"/>
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string LinuxToWindows(string s)
        {
            var sshHelper = CurrentFromActiveDoc();
            if (sshHelper == null || sshHelper.SftpClient == null)
                throw new Renci.SshNet.Common.SshConnectionException("No Connection");

            Regex regexAdvanced = new Regex(@"(\/mnt\/([a-z])\/)");

            s = s.Replace("~", sshHelper.LinuxHomeReplacement);


            string Replacers(Match matchResult)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(matchResult.Groups[2].Value.ToUpper());
                sb.Append(":\\");

                if (matchResult.Groups[3].Success)
                {
                    sb.Append(matchResult.Groups[3].Value.Replace("/", "\\"));
                }
                return sb.ToString();

            }

            return regexAdvanced.Replace(s.Replace('−', '-').Replace(sshHelper.LinuxHome.Replace("~", sshHelper.LinuxHomeReplacement), sshHelper.SftpHome), new MatchEvaluator((v) => Replacers(v)));
        }

        /// <summary>
        /// Replaces C: to /mnt/c and replaces <see cref="SSH_Helper.WinHome"/>
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string WindowsToLinux(string s)
        {
            if (CurrentFromActiveDoc() == null || CurrentFromActiveDoc().SftpClient == null)
                throw new Renci.SshNet.Common.SshConnectionException("No Connection");

            Regex regexAdvanced = new Regex(@"([a-zA-Z]):\\(.*)", RegexOptions.Compiled);

            string Replacers(Match matchResult)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("/mnt/");
                sb.Append(matchResult.Groups[1].Value.ToLower());
                if (matchResult.Groups[2].Success)
                {
                    sb.Append("/");
                    sb.Append(matchResult.Groups[2].Value.Replace("\\", "/"));
                }
                return sb.ToString();

            }
            var sshHelper = CurrentFromActiveDoc();
            return regexAdvanced.Replace(s.Replace('−', '-').Replace(sshHelper.WindowsParentPath, sshHelper.LinuxParentPath), new MatchEvaluator((v) => Replacers(v))).Replace("\\", "/");
        }



        public static string ToSftpPath(this string s, PathType pathType = PathType.Unknown)
        {
            pathType = pathType == PathType.Unknown ? GuessType(s) : pathType;
            if (pathType == PathType.Sftp) return s.Replace('−', '-');

            var sshHelper = CurrentFromActiveDoc();

            if (sshHelper == null || sshHelper.SftpClient == null)
                throw new Renci.SshNet.Common.SshConnectionException("No Connection");

            if (s == null)
                return null;

            switch (pathType)
            {
                case PathType.Linux:
                    return "/" + LinuxToWindows(s).Replace("\\","/");


                case PathType.Windows:
                default:
                    return "/" + s.Replace("\\", "/");

            }



        }

        public static string ToLinuxPath(this string s, PathType pathType = PathType.Unknown)
        {
            pathType = pathType == PathType.Unknown ? GuessType(s) : pathType;
            if (pathType == PathType.Sftp) return s.Replace('−', '-');

            var sshHelper = CurrentFromActiveDoc();

            if (sshHelper == null || sshHelper.SftpClient == null)
                throw new Renci.SshNet.Common.SshConnectionException("No Connection");

            if (s == null)
                return null;

            switch (pathType)
            {
                case PathType.Sftp:
                    return WindowsToLinux(s);

                case PathType.Windows:
                default:
                    return WindowsToLinux(s);



            }

        }

        /// <summary>
        /// If the input type is sftp, it does NOT change the paths such as local/remote, it merely converts the syntax.
        /// If the input type is Linux it will TRY to change the path 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="pathType"></param>
        /// <returns></returns>
        /// <exception cref="Renci.SshNet.Common.SshConnectionException">if no connection</exception>
        public static string ToWindowsPath(this string s, PathType pathType = PathType.Unknown)
        {

            pathType = pathType == PathType.Unknown ? GuessType(s) : pathType;
            if (pathType == PathType.Windows) return s.Replace('−', '-');

            var sshHelper = CurrentFromActiveDoc();

            if (sshHelper == null || sshHelper.SftpClient == null)
                throw new Renci.SshNet.Common.SshConnectionException("No Connection");

            if (s == null)
                return null;

            if (Path.GetInvalidPathChars().Any(c => s.Contains(c)))
            {
                throw new ArgumentException($"Illegal characters in path {s}");

            }

            switch (pathType)
            {
                case PathType.Sftp:
                    return s.Substring(1, s.Length - 1).Replace("/", "\\").Replace('−', '-');

                case PathType.Linux:
                default:
                    return LinuxToWindows(s.Replace(sshHelper.LinuxParentPath, sshHelper.WindowsParentPath).Replace("/", "\\").Replace('−', '-'));

            }

        }

    }
}

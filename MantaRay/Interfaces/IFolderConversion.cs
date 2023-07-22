using MantaRay.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Interfaces
{
    /// <summary>
    /// Used for the <see cref="SSH_Helper"/> to map linux vs windows paths
    /// </summary>
    public interface IFolderConversion
    {
        string LinuxHome { get; }
        string SftpHome { get; }
        string WinHome { get; }
    }
}

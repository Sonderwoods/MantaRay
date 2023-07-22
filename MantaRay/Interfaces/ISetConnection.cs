using MantaRay.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Interfaces
{
    public interface ISetConnection
    {
        SSH_Helper SshHelper { get; }
    }
}

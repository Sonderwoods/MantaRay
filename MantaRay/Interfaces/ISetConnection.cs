using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    public interface ISetConnection
    {
        SSH_Helper SshHelper { get; }
    }
}

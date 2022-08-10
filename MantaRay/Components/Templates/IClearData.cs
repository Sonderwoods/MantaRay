using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    public interface IClearData
    {
        /// <summary>
        /// Here we clear persistant data such as results, runtimes etc.
        /// </summary>
        void ClearCachedData();
    }
}

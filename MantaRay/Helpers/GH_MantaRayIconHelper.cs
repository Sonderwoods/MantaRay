using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    public class GH_MantaRayIconHelper: GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Ray", Resources.Resources.Ra_IconDark);
            Instances.ComponentServer.AddCategorySymbolName("Ray", 'R');
            return GH_LoadingInstruction.Proceed;
        }
    }

}

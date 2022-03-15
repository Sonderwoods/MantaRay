using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace GrasshopperRadianceLinuxConnector
{
    public abstract class GH_Template : GH_Component
    {

        public GH_Template(string name, string nickname, string description, string subcategory = "Test") :
            base(name, nickname, description, "Rad", subcategory)
        {
            

        }

        protected override System.Drawing.Bitmap Icon => Resources.Resources.Ra_Icon;

    }
}

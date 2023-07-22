using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using MantaRay.Setup;

namespace MantaRay.Components.Templates
{
    public abstract class GH_Template : GH_Component
    {

        bool checkedForUpdate = false;

        public GH_Template(string name, string nickname, string description, string subcategory = "Test") :
            base(name, nickname, description + $"\n\nPart of {ConstantsHelper.ProjectName} by Mathias Sønderskov Schaltz, 2022\nVersion 1.0.0.2-alpha", "Ray", subcategory)


        {

        }



        protected override void BeforeSolveInstance()
        {
            if (!checkedForUpdate)
            {
                checkedForUpdate = true;
                Helpers.ReplaceMissingComponentsHelper.FixAbsolete(this);

            }
            base.BeforeSolveInstance();
        }

        protected override System.Drawing.Bitmap Icon => Resources.Resources.Ra_Icon;

    }
}

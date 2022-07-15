using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace MantaRay
{
    public class MantaRayInfo : GH_AssemblyInfo
    {
        public override string Name => "MantaRay";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Project MantaRay is a Grasshopper Radiance Linux Connector";

        public override Guid Id => new Guid("EF49250E-2BA1-415D-9FC6-284358354119");

        //Return a string identifying you or your company.
        public override string AuthorName => "Mathias Sønderskov Schaltz";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "Find me on LinkedIn";
    }
}
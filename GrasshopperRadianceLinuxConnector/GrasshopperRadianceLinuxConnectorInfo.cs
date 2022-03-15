﻿using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace GrasshopperRadianceLinuxConnector
{
    public class GrasshopperRadianceLinuxConnectorInfo : GH_AssemblyInfo
    {
        public override string Name => "GrasshopperRadianceLinuxConnector";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("EF49250E-2BA1-415D-9FC6-284358354119");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}
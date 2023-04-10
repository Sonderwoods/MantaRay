using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Runtime;

namespace MantaRay.Components
{
    public class GH_DeconstructModifier : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_DeconstructModifier class.
        /// </summary>
        public GH_DeconstructModifier()
          : base("Get Modifier Names", "Mod Names",
              "Deconstructs the modifier string to get a name and suggests a name for geometry\n\nThe strings will be cleaned to only include [^a-zA-Z0-9_.]",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Modifier", "Mod", "Modifier string", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("GeoName", "GeoName", "Suggested Geometry Name", GH_ParamAccess.item);
            pManager.AddTextParameter("ModifierName", "ModName", "Modifier Name", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Regex regexAdvanced = new Regex(@"[^a-zA-Z0-9_.]>", RegexOptions.Compiled);


            var input = DA.Fetch<string>(this, 0);
            if (!string.IsNullOrEmpty(input))
            {
                DA.SetData(0, regexAdvanced.Replace(GetModName(input), "_") + "_geo");
                DA.SetData(1, regexAdvanced.Replace(GetModName(input), "_"));

            }

        }


        public string GetModName(string modifier)
        {
            try
            {
                return modifier.Split('\n').First().Split(' ').Skip(2).First();

            }
            catch
            {
                return "";
            }

        }
       
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B5AD5CDF-21D2-4327-B4CE-F3EFFC2CE927"); }
        }
    }
}
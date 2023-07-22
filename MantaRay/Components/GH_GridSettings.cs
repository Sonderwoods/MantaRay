using Grasshopper.Kernel;
using MantaRay.Components.Templates;
using MantaRay.Helpers;
using MantaRay.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace MantaRay.Components
{
    /// <summary>
    /// Original (C) Henning Larsen Architects 2019
    /// GPL License
    /// Author: Mathias Sønderskov
    /// https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public
    /// </summary>
    public class GH_GridSettings : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_SectionType_Range class.
        /// </summary>
        public GH_GridSettings()
          : base("GridSettings", "GridSettings",
              "Description",
               "2 Radiance")
        {
        }

        //public override bool IsPreviewCapable => false;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("From", "From", "From", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("To", "To", "To", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("A - Steps", "A - Steps", "Steps. Only use this one OR stepsize OR steps!", GH_ParamAccess.item, -1);
            pManager.AddNumberParameter("B - StepSize", "B - StepSize", "StepSize.  Only use this one OR stepsize OR steps!", GH_ParamAccess.item, -1);
            pManager.AddNumberParameter("C - Steps", "C - Steps", "manually input steps. Only use this one OR stepsize OR steps!", GH_ParamAccess.list, new List<double>());

            for (int i = 0; i < pManager.ParamCount; i++)
            {
                pManager[i].Optional = true;
            }



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Input Selector", "Input Selector", "Input Selector", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double? from = DA.Fetch<double?>(this, "From");
            double? to = DA.Fetch<double?>(this, "To");
            int steps = DA.Fetch<int>(this, "A - Steps");
            double stepSize = DA.Fetch<double>(this, "B - StepSize");
            List<double> manuallySteps = DA.FetchList<double>(this, "C - Steps");

            if (steps > 0 && stepSize > 0 ||
                steps > 0 && manuallySteps.Count > 0 ||
                stepSize > 0 && manuallySteps.Count > 0)
            {
                throw new Exception("Unsure whether you want to A, B or C.");
            }

            GridTypeSelector inputSelector = null;

            if (steps > 0)
                inputSelector = new GridTypeSelector(steps, from, to);

            if (stepSize > 0)
                inputSelector = new GridTypeSelector(stepSize, from, to);

            if (manuallySteps.Count > 0)
                inputSelector = new GridTypeSelector(manuallySteps);

            DA.SetData(0, inputSelector);
        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("53a7e189-6118-4ff2-be6d-68362245ed08"); }
        }
    }
}
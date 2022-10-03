using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MantaRay.Components.Templates
{
    public class GH_TestComponentColor : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_TestComponentColor class.
        /// </summary>
        public GH_TestComponentColor()
          : base("TestComponentColor", "Component Color",
              "Test colors of a component... For Development",
              "Test")
        {
        }

        public override void CreateAttributes()
        {
            //base.CreateAttributes();
            m_attributes = new GH_TestComponentColor_Attr(this);

        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "Col", "Col", GH_ParamAccess.item);
            pManager[pManager.AddColourParameter("SelColor", "SelCol", "Col", GH_ParamAccess.item)].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ((GH_TestComponentColor_Attr)m_attributes).Color = DA.Fetch<System.Drawing.Color>(this, "Color");
            ((GH_TestComponentColor_Attr)m_attributes).ColorSelected = DA.Fetch<System.Drawing.Color>(this, "SelColor");
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6A4DEF5C-7EB4-4490-B233-3C630BED68D1"); }
        }
    }
}
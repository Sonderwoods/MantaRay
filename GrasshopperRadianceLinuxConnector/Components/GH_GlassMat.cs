using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_GlassMat : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_OpaqueMat class.
        /// </summary>
        public GH_GlassMat()
          : base("GlassMat", "GlassMat",
              "Creates a glass material",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name", GH_ParamAccess.list);
            pManager.AddNumberParameter("Transmittance[0-1]", "Transmittance[0-1]", "Transmittance (ie VLT value)", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("RadMaterials", "RadMaterials", "RadMaterials", GH_ParamAccess.item);
            pManager.AddTextParameter("RadMaterialList", "RadMaterialList", "RadMaterialList", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var names = DA.FetchList<string>("Name");
            var transmittances = DA.FetchList<double>("Transmittance[0-1]");
      

            int count = names.Count;

            List<string> materialList = new List<string>(count);

            if (transmittances.Count != count)
            {
                throw new Exception("Wrong number of items in the inputs. They must match or be == 1");
            }

     


            for (int i = 0; i < names.Count; i++)
            {
                var name = names[i].AddGlobals().Cleaned();
                var transmittance = transmittances[i];
      

                if (transmittance > 1.0)
                    transmittance /= 100.0;

                if (transmittance >= 0.88)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Transmittance above 0.88 are uncommon");

                if (transmittance < 0.3)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Transmittance below 0.3 are uncommon");


                materialList.Add($"void glass {name}\n" +
                $"0\n" +
                $"0\n" +
                $"3 {TransmittanceToTransmissivity(transmittance):0.000} {TransmittanceToTransmissivity(transmittance):0.000} {TransmittanceToTransmissivity(transmittance):0.000}");
            }

            DA.SetData(0, String.Join("\n", materialList));
            DA.SetDataList(1, materialList);


        }

        public static double TransmittanceToTransmissivity(double t)
        {
            return (Math.Sqrt(.8402528435 + .0072522239 * t * t) - .9166530661) / .0036261119 / t;
        }



        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("91D26C40-176B-4A3F-B7B2-4A61575B928F"); }
        }
    }
}
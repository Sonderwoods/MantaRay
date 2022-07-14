using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperRadianceLinuxConnector.Components;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector.Components
{
    public class GH_OpaqueGreyMat : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_OpaqueMat class.
        /// </summary>
        public GH_OpaqueGreyMat()
          : base("OpaqueGreyMat", "OpaqueGreyMat",
              "Creates a plastic material",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name", GH_ParamAccess.list);
            pManager.AddNumberParameter("Reflectance[0-1]", "Reflectance[0-1]", "Reflectance", GH_ParamAccess.list);
            pManager[pManager.AddNumberParameter("Specularity", "Specularity", "spec", GH_ParamAccess.list, 0.0)].Optional = true;
            pManager[pManager.AddNumberParameter("Roughness", "Roughness", "Roughness", GH_ParamAccess.list, 0.0)].Optional = true;
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
            var reflectances = DA.FetchList<double>("Reflectance[0-1]");
            var specularities = DA.FetchList<double>("Specularity");
            var roughnesses = DA.FetchList<double>("Roughness");

            int count = names.Count;

            List<string> materialList = new List<string>(count);

            if ((reflectances.Count != count) ||
                (roughnesses.Count > 1 && roughnesses.Count != count) ||
                (specularities.Count > 1 && specularities.Count != count))
            {
                throw new Exception("Wrong number of items in the inputs. They must match or be == 1");
            }

            if (specularities.Count == 1)
                while (specularities.Count < count)
                    specularities.Add(specularities[0]);

            if (roughnesses.Count == 1)
                while (roughnesses.Count < count)
                    roughnesses.Add(roughnesses[0]);


            for (int i = 0; i < names.Count; i++)
            {
                var name = names[i].AddGlobals().Cleaned();
                var reflectance = reflectances[i];
                var roughness = roughnesses[i];
                var specularity = specularities[i];

                if (reflectance > 1.0)
                    reflectance /= 100.0;

                if (reflectance >= 0.9)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Reflectances above 0.9 are uncommon");

                if (reflectance < 0.1)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Reflectances below 0.1 are uncommon");

                if (specularity >= 0.1)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Specularity above 0.1 are uncommon");

                if (roughness >= 0.2)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Roughness above 0.2 are uncommon");


                materialList.Add($"void plastic {name}\n" +
                $"0\n" +
                $"0\n" +
                $"5 {reflectance:0.000} {reflectance:0.000} {reflectance:0.000} {specularity:0.000} {roughness:0.000}");
            }

            DA.SetData(0, String.Join("\n", materialList));
            DA.SetDataList(1, materialList);


        }


        protected override Bitmap Icon => Resources.Resources.Ra_Mat_Icon;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("91D26C40-176B-4A3F-A7B2-4A61575B948F"); }
        }
    }
}
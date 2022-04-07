using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public class GH_MeshToObj : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_MeshToRad class.
        /// </summary>
        public GH_MeshToObj()
          : base("MeshToObj", "Mesh2Obj",
              "MeshToRad. Heavily inspired by\n" +
                "https://github.com/ladybug-tools/honeybee-legacy/blob/master/userObjects/Honeybee_MSH2RAD.ghuser\n" +
                "CAUTION: Does not export any UV mapping of materials etc. Just applies the modifer that you input.\n" +
                "Connect me to the ObjToRad component for rad files.",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.tree); //TODO: change to tree and allow parallel runs
            pManager.AddTextParameter("Name", "Name", "Name (will save name.rad)", GH_ParamAccess.tree);
            pManager.AddTextParameter("ModifierName", "ModifierName", "ModifierName - Name of the radiance material", GH_ParamAccess.tree);
            pManager[pManager.AddTextParameter("Subfolder Override", "Subfolder", "Optional. Override the subfolder from the connection component.\n" +
                "Example:\n" +
                "simulation/objFiles", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Obj Files", "Obj Files", "path for exported obj files. Full local windows path. Output me into the obj2rad component.", GH_ParamAccess.list);
            pManager.AddTextParameter("Map File", "Map File", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.tree);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Read and parse the input.
            var runTree = new GH_Structure<GH_Boolean>();
            runTree.Append(new GH_Boolean(DA.Fetch<bool>("Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>("Run"))
                return;

            string workingDir;

            string subfolder = DA.Fetch<string>("Subfolder Override").AddGlobals().Replace('/', '\\').Trim('\\'); //keep backslash as we're in windows.

            Grasshopper.Kernel.Data.GH_Structure<GH_Mesh> inMeshes = DA.FetchTree<GH_Mesh>("Mesh");

            Grasshopper.Kernel.Data.GH_Structure<GH_String> names = DA.FetchTree<GH_String>("Name");

            Grasshopper.Kernel.Data.GH_Structure<GH_String> modifierNames = DA.FetchTree<GH_String>("ModifierName");

            if (modifierNames.Branches.Count != inMeshes.Branches.Count || inMeshes.Branches.Count != names.Branches.Count)
            {
                throw new ArgumentOutOfRangeException(String.Format("Meshes ({0}), Names ({1}) and ModifierNames ({2}) have different branch count.",
                    inMeshes.Branches.Count,
                    names.Branches.Count,
                    modifierNames.Branches.Count));
            }

            

            if (string.IsNullOrEmpty(subfolder))
            {
                workingDir = SSH_Helper.WindowsFullpath;
            }
            else
            {
                workingDir = SSH_Helper.WindowsParentPath + @"\" + subfolder;
            }

            workingDir = (workingDir.EndsWith(@"\") || workingDir.EndsWith("/")) ? workingDir : workingDir + @"\";

            string mappingFilePath = $"{workingDir}mapping.map";

            
            object myLock = new object();


            // Write the mapping.map file

            StringBuilder mapping = new StringBuilder("default;");

            List<string> localFilePaths = new List<string>(inMeshes.Branches.Count);

            

            for (int i = 0; i < inMeshes.Branches.Count; i++)
            {

                string modifierName = modifierNames[i][0].Value.Replace(" ", "_");

                string name = names[i][0].Value.Replace(" ", "_");

                mapping.AppendFormat("\n{0} (Group \"{1}\");", modifierName.AddGlobals(), name.AddGlobals());

                localFilePaths.Add(workingDir + name + ".obj");
            }

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(mappingFilePath)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(mappingFilePath));

            System.IO.File.WriteAllText(mappingFilePath, mapping.ToString());



            // Write an obj file for each branch in the meshes list

            //Parallel.For(0, inMeshes.Branches.Count, q =>
            //{
            for (int q = 0; q < inMeshes.Branches.Count; q++)
            {


                string name = names[q][0].Value.Replace(" ", "_"); //TODO: more fixes?

                string geometryFilePath = workingDir + $"{name}.obj";

                StringBuilder geometryFile = new StringBuilder();

                geometryFile.Append("# Written with GrasshopperRadianceLinuxConnector/GH_MeshToRad\r\n");

                geometryFile.AppendFormat("g {0}\r\n", name);

                foreach (GH_Mesh gmesh in inMeshes[q])
                {
                    Mesh mesh = gmesh.Value;

                    for (int j = 0; j < mesh.Vertices.Count; j++)
                    {
                        geometryFile.AppendFormat("v {0:0.000} {1:0.000} {2:0.000}\r\n", mesh.Vertices[j].X, mesh.Vertices[j].Y, mesh.Vertices[j].Z);
                        //TODO: Tolerances/Units?
                    }

                    for (int j = 0; j < mesh.Faces.Count; j++)
                    {
                        if (mesh.Faces[j].IsQuad)
                        {
                            geometryFile.AppendFormat("f {0} {1} {2} {3}\r\n", mesh.Faces[j].A+1, mesh.Faces[j].B+1, mesh.Faces[j].C+1, mesh.Faces[j].D+1);
                        }
                        else
                        {
                            geometryFile.AppendFormat("f {0} {1} {2}\r\n", mesh.Faces[j].A+1, mesh.Faces[j].B+1, mesh.Faces[j].C+1, mesh.Faces[j].D+1);
                        }
                    }
                }

                System.IO.File.WriteAllText(geometryFilePath, geometryFile.ToString());
            }
            //});


            DA.SetDataList("Obj Files", localFilePaths);
            DA.SetData("Map File", mappingFilePath);






        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7262DFF6-5027-40E7-A493-840F258EFB83"); }
        }
    }
}
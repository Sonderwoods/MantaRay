using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MantaRay.Components;
using MantaRay.Helpers;
using MantaRay.Setup;
using Rhino.Geometry;

namespace MantaRay.Old

{
    [Obsolete]
    public class GH_MeshToObj_OBSOLETE : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_MeshToRad class.
        /// </summary>
        public GH_MeshToObj_OBSOLETE()
          : base("MeshToObj", "Mesh2Obj",
              "MeshToRad.\n\n" +

                "CAUTION: Does not export any UV mapping of materials etc. Just applies the modifer that you input.\n" +
                "Connect me to the ObjToRad component for rad files.\n\n" +
                "It is advised to join large list of meshes into singular joined meshes.\n" +
                "IE instead of 300 wall meshes, join them into one wall mesh.\n" +
                "The component runs in parallel if you graft the inputs. So graft a tree with x lists when you have x materials/modifiers.\n\n" +
                "Units: All obj files will be scaled from rhino units to meters",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager[pManager.AddMeshParameter("Mesh", "Mesh", "Mesh. It's advisable to have same material meshes joined before entering this component.\n" +
                "And if you graft the input per material then it will run in parallel.\n" +
                "Example: you have 3 objects:  floor/ceiling/wall\n" +
                "then you should join all your floor meshes into one joined floor mesh, same for the others.\n" +
                "And input it as a grafted list. This will make the component run 3 meshing engines at the same time.", GH_ParamAccess.tree)].DataMapping = GH_DataMapping.Graft;

            pManager[pManager.AddTextParameter("Name", "Name", "Name (will save name.rad)", GH_ParamAccess.tree)].DataMapping = GH_DataMapping.Graft;

            pManager[pManager.AddTextParameter("MapFileName", "MapFileName", "name of mapping file name. Default is mapping (.map)", GH_ParamAccess.item, "mapping")].Optional = true;
            pManager[pManager.AddTextParameter("ModifierName", "ModifierName", "ModifierName - Name of the radiance material", GH_ParamAccess.tree)].DataMapping = GH_DataMapping.Graft;

            pManager[pManager.AddTextParameter("Target folder", "Target folder", "Optional. Override the subfolder from the connection component.\n" +
                "Example:\n" +
                "simulation/objFiles", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

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
            runTree.Append(new GH_Boolean(DA.Fetch<bool>(this, "Run")));
            Params.Output[Params.Output.Count - 1].ClearData();
            DA.SetDataTree(Params.Output.Count - 1, runTree);

            if (!DA.Fetch<bool>(this, "Run"))
                return;

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());

            string workingDir;

            string mappingName = DA.Fetch<string>(this, "MapFileName");

            string subfolder = DA.Fetch<string>(this, "Target folder", "Subfolder Override").ApplyGlobals().Replace('/', '\\').Trim('\\'); //keep backslash as we're in windows.

            Grasshopper.Kernel.Data.GH_Structure<GH_Mesh> inMeshes = DA.FetchTree<GH_Mesh>(this, "Mesh");

            Grasshopper.Kernel.Data.GH_Structure<GH_String> names = DA.FetchTree<GH_String>(this, "Name");

            Grasshopper.Kernel.Data.GH_Structure<GH_String> modifierNames = DA.FetchTree<GH_String>(this, "ModifierName");

            if (modifierNames.Branches.Count != inMeshes.Branches.Count || inMeshes.Branches.Count != names.Branches.Count)
            {
                throw new ArgumentOutOfRangeException(String.Format("Meshes ({0}), Names ({1}) and ModifierNames ({2}) have different branch count.",
                    inMeshes.Branches.Count,
                    names.Branches.Count,
                    modifierNames.Branches.Count));
            }




            if (string.IsNullOrEmpty(subfolder))
            {
                workingDir = sshHelper.WinHome;
            }
            else
            {
                workingDir = sshHelper.WindowsParentPath + @"\" + subfolder;
            }

            workingDir = (workingDir.EndsWith(@"\") || workingDir.EndsWith("/")) ? workingDir : workingDir + @"\";

            string mappingFilePath = $"{workingDir}{mappingName}.map";


            // Write the mapping.map file

            StringBuilder mapping = new StringBuilder("default;");

            List<string> localFilePaths = new List<string>(inMeshes.Branches.Count);



            for (int i = 0; i < inMeshes.Branches.Count; i++)
            {
                if (names[i].Count > 1 || modifierNames[i].Count > 1)
                {
                    throw new Exception($"Please only use one modifier and one name per list input.\nThis is wrong in tree branch [{i}]");
                }

                string modifierName = modifierNames[i][0].Value.Replace(" ", "_");

                string name = names[i][0].Value.Replace(" ", "_");

                mapping.AppendFormat("\n{0} (Group \"{1}\");", modifierName.ApplyGlobals(), name.ApplyGlobals());

                localFilePaths.Add(workingDir + name + ".obj");
            }

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(mappingFilePath)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(mappingFilePath));

            System.IO.File.WriteAllText(mappingFilePath, mapping.ToString());

            Transform unitScaler = Transform.Scale(new Point3d(0, 0, 0), 1.0.ToMeter());



            // Write an obj file for each branch in the meshes list
            try
            {
                Parallel.For(0, inMeshes.Branches.Count, q =>
                {
                    //for (int q = 0; q < inMeshes.Branches.Count; q++)
                    //{

                    if (inMeshes.Branches[q].Count > 10000)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Long list of meshes.\nAre you sure you're not better off joining the meshes first?");
                    }


                    string name = names[q][0].Value.Replace(" ", "_"); //TODO: more fixes?

                    string geometryFilePath = workingDir + $"{name}.obj";

                    StringBuilder geometryFile = new StringBuilder();

                    geometryFile.Append($"# Written with {ConstantsHelper.ProjectName} plugin/GH_MeshToRad\r\n");

                    geometryFile.AppendFormat("g {0}\r\n", name);

                    int totalVertexCount = 1; // vertices in obj files start at #1
                    int objectCounter = 0;



                    for (int i = 0; i < inMeshes[q].Count; i++)
                    {
                        int currentVertices = 0;
                        Mesh mesh = inMeshes[q][i].Value.DuplicateMesh();
                        mesh.Transform(unitScaler);
                        //mesh.Faces.ConvertQuadsToTriangles();
                        mesh.FaceNormals.ComputeFaceNormals();

                        geometryFile.AppendFormat(CultureInfo.InvariantCulture, "o {0}_{1:0}\r\n", name, ++objectCounter);

                        for (int j = 0; j < mesh.Vertices.Count; j++)
                        {
                            currentVertices++;
                            geometryFile.AppendFormat(CultureInfo.InvariantCulture, "v {0:0.000} {1:0.000} {2:0.000}\r\n", mesh.Vertices[j].X, mesh.Vertices[j].Y, mesh.Vertices[j].Z);
                            //TODO: Tolerances/Units?
                        }

                        for (int j = 0; j < mesh.Faces.Count; j++)
                        {
                            if (mesh.Faces[j].IsQuad)
                            {
                                if (!InverseVertexOrder(mesh, j))
                                {
                                    geometryFile.AppendFormat(CultureInfo.InvariantCulture, "f {0} {1} {2} {3}\r\n", mesh.Faces[j].A + totalVertexCount, mesh.Faces[j].B + totalVertexCount, mesh.Faces[j].C + totalVertexCount, mesh.Faces[j].D + totalVertexCount);
                                }
                                else
                                {
                                    geometryFile.AppendFormat(CultureInfo.InvariantCulture, "f {0} {1} {2} {3}\r\n", mesh.Faces[j].D + totalVertexCount, mesh.Faces[j].C + totalVertexCount, mesh.Faces[j].B + totalVertexCount, mesh.Faces[j].A + totalVertexCount);

                                }
                            }
                            else
                            {
                                if (!InverseVertexOrder(mesh, j))
                                {
                                    geometryFile.AppendFormat(CultureInfo.InvariantCulture, "f {0} {1} {2}\r\n", mesh.Faces[j].A + totalVertexCount, mesh.Faces[j].B + totalVertexCount, mesh.Faces[j].C + totalVertexCount);
                                }
                                else
                                {
                                    geometryFile.AppendFormat(CultureInfo.InvariantCulture, "f {0} {1} {2}\r\n", mesh.Faces[j].C + totalVertexCount, mesh.Faces[j].B + totalVertexCount, mesh.Faces[j].A + totalVertexCount);
                                }
                            }
                        }
                        totalVertexCount += currentVertices;
                    }
                    if (!File.Exists(geometryFilePath) || !IsFileLocked(new FileInfo(geometryFilePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(geometryFilePath));
                        System.IO.File.WriteAllText(geometryFilePath, geometryFile.ToString());
                    }
                    else
                    {
                        throw new AccessViolationException($"File is locked? {geometryFilePath}");
                    }

                    //}
                });
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    throw e;
                }
            }


            DA.SetDataList("Obj Files", localFilePaths);
            DA.SetData("Map File", mappingFilePath);






        }

        public static bool InverseVertexOrder(Mesh mesh, in int faceIndex)
        {
            var face = mesh.Faces[faceIndex];


            Vector3f normalFromVertices = face.IsQuad ?
                NormalFromVertices(mesh.Vertices[face.A], mesh.Vertices[face.B], mesh.Vertices[face.B], mesh.Vertices[face.C])
                : NormalFromVertices(mesh.Vertices[face.A], mesh.Vertices[face.B], mesh.Vertices[face.B]);

            Vector3f normalFromFace = mesh.FaceNormals[faceIndex];

            double angle = Math.Asin(

                Vector3f.CrossProduct(normalFromFace, normalFromVertices).Length
                /
                (normalFromFace.Length * normalFromVertices.Length)
            );

            return angle > 0;



        }

        public static Vector3f NormalFromVertices(in Point3f pt0, in Point3f pt1, in Point3f pt2)
        {
            Vector3f normalFromVertices = Vector3f.CrossProduct(pt1 - pt0, pt2 - pt0);
            normalFromVertices.Unitize();
            return normalFromVertices;
        }

        public static Vector3f NormalFromVertices(in Point3f pt0, in Point3f pt1, in Point3f pt2, in Point3f pt3)
        {
            Vector3f n1 = Vector3f.CrossProduct(pt1 - pt0, pt2 - pt0);
            n1.Unitize();

            Vector3f n2 = Vector3f.CrossProduct(pt3 - pt2, pt1 - pt2);
            n2.Unitize();

            return 0.5f * (n1 + n2);


        }

        protected override Bitmap Icon => Resources.Resources.Ra_Obj_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7262DFF6-5027-40E7-A493-840F258EFB83"); }
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}
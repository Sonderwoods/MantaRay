using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public class MeshToRadHelper
    {
        public string WorkingDirectory { get; set; }
        public string Name { get; set; }
        public string RadianceMaterial { get; set; }
        public Mesh Mesh { get; set; }
        public MeshToRadHelper(Mesh mesh, string fileName, string workingDir, string modifier)
        {
            Mesh = mesh;
            WorkingDirectory = workingDir;
            Name = fileName;
            RadianceMaterial = modifier;

        }

        /// <summary>
        /// Exports the meshes to obj files
        /// </summary>
        /// <returns></returns>
        public List<string> MeshToObj()
        {
            List<string> radFiles = new List<string>(2);

            // Using translated TurtlePyMesh

            StringBuilder outfile = new StringBuilder();
            outfile.AppendLine("# OBJ file written by TurtlePyMesh translated to C# by With the GrasshopperRadianceLinuxConnector\n\n");

            foreach (var item in Mesh.Faces)
            {

            }


            return radFiles;
        }
    }
}

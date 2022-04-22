using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public static class MeshToRadHelper
    {

        
        public static bool MeshNormalMatchesVertexOrder(Mesh mesh, in int faceIndex)
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


    }
}

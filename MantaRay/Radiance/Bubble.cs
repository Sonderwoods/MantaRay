using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Radiance
{
    /// <summary>
    /// An inverse sphere
    /// </summary>
    public class Bubble : Sphere
    {
        public Bubble(string[] data) : base(data, true) //<-- true on the boolean means invert normals
        {

        }
    }
}

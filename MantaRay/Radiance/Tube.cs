using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Radiance
{
    /// <summary>
    /// An inverse cylinder
    /// </summary>
    public class Tube : Cylinder
    {
        public Tube(string[] data) : base(data, true)
        {

        }
    }
}

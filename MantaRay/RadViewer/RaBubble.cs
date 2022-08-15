using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer
{
    /// <summary>
    /// An inverse sphere
    /// </summary>
    public class RaBubble : RaSphere
    {
        public RaBubble(string[] data) : base(data, true) //<-- true on the boolean means invert normals
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.RadViewer
{

    public class RadianceMaterial : RadianceObject
    {
        public string MaterialDefinition { get; set; }

        public RadianceMaterial(string[] data) : base(data)
        {
            //IEnumerable<string> dataNoHeader = data.Skip(6);
            MaterialDefinition = String.Join(" ", data.Take(3)) + "\n" + String.Join("\n", data.Skip(3).Take(3)) + "\n" + String.Join(" ", data.Skip(6));
        }
    }
}

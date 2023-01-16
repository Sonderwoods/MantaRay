using MantaRay.Radiance.HeadsUpDisplay;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel;

namespace MantaRay.Radiance
{

    public abstract class RadianceObject
    {
        public virtual string ModifierName => modifierName;
        public virtual string Type => type;
        public virtual string Name => name;

        public RadianceObject Modifier;

        string modifierName;
        string type;
        string name;


        public RadianceObject(string[] data)
        {
            modifierName = data[0];
            type = data[1];
            name = data[2];
        }

        public RadianceObject()
        {

        }

        [Pure]
        public static RadianceObject FromString(string line)
        {
            const string rep_new_line_re = @"/\s\s+/g";

            string[] data = Regex.Replace(line, rep_new_line_re, " ").Trim().Split(' ').Where(d => !String.IsNullOrEmpty(d)).ToArray();

            if (data.Length < 3)
                return null;

            string type = data[1];

            if (type.Length == 0)
                return null;

            switch (type)
            {
                case "polygon":
                    return new Polygon(data);
                case "sphere":
                    return new Sphere(data);
                case "cylinder":
                    return new Cylinder(data);
                case "tube":
                    return new Tube(data);
                case "bubble":
                    return new Bubble(data);
                case "cone":
                case "plastic":
                case "glass":
                case "metal":
                case "trans":
                case "glow":
                case "mirror":
                case "bsdf":
                default:
                    return new Material(data);
            }

        }


    }
}

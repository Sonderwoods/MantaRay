using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrasshopperRadianceLinuxConnector
{
    public static class UnitHelper
    {
        private static double GetConversionFactor()
        {
            string units = Rhino.RhinoDoc.ActiveDoc.GetUnitSystemName(true, true, true, true); ;
            double q;
            switch (units)
            {
                case "m":
                    q = 1.0;
                    break;
                case "mm":
                    q = 1000.0;
                    break;
                case "cm":
                    q = 100.0;
                    break;
                case "ft":
                    q = 304.8 / 1000.0;
                    break;
                default:
                    throw new Exception("unknown units");
            }
            return q;
        }

        public static double FromMeter(this double length)
        {
            return length * GetConversionFactor();
        }

        public static double ToMeter(this double length)
        {
            return length / GetConversionFactor();
        }
    }
}

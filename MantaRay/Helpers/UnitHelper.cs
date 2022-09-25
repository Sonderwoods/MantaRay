using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Helpers
{
    public static class UnitHelper
    {
        public static double GetConversionFactor()
        {
            switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
            {
                case Rhino.UnitSystem.Meters:
                    return 1;
                case Rhino.UnitSystem.Decimeters:
                    return 1e1;
                case Rhino.UnitSystem.Centimeters:
                    return 1e2;
                case Rhino.UnitSystem.Millimeters:
                    return 1e3;
            }

            double factor = 1;
            switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
            {
                case Rhino.UnitSystem.None:
                    factor = 0;//No unit system
                    break;

                case Rhino.UnitSystem.Microns:
                    factor = 1e-6; //1.0e-6 meters
                    break;

                case Rhino.UnitSystem.Millimeters:
                    factor = 1e-3; //1.0e-3 meters
                    break;

                case Rhino.UnitSystem.Centimeters:
                    factor = 1e-2; //1.0e-2 meters
                    break;

                case Rhino.UnitSystem.Meters:
                    factor = 1;
                    break;

                case Rhino.UnitSystem.Kilometers:
                    factor = 1e3; //1.0e+3 meters
                    break;

                case Rhino.UnitSystem.Microinches:
                    factor = 2.54e-8; //2.54e-8 meters, 1.0e-6 inches
                    break;

                case Rhino.UnitSystem.Mils:
                    factor = 2.54e-5; //2.54e-5 meters, 0.001 inches
                    break;

                case Rhino.UnitSystem.Inches:
                    factor = 0.0254; //0.0254 meters
                    break;

                case Rhino.UnitSystem.Feet:
                    factor = 0.3408; //0.3408 meters, 12 inches
                    break;

                case Rhino.UnitSystem.Miles:
                    factor = 1e0;//(1609.344 meters, 5280 feet)
                    break;


                case Rhino.UnitSystem.Angstroms:
                    factor = 1.0e-10; //1.0e-10 meters
                    break;

                case Rhino.UnitSystem.Nanometers:
                    factor = 1.0e-9; //1.0e-9 meters
                    break;

                case Rhino.UnitSystem.Decimeters:
                    factor = 1.0e-1; //1.0e-1 meters
                    break;

                case Rhino.UnitSystem.Dekameters:
                    factor = 1.0e+1; //1.0e+1 meters
                    break;

                case Rhino.UnitSystem.Hectometers:
                    factor = 1.0e+2; //1.0e+2 meters
                    break;

                case Rhino.UnitSystem.Megameters:
                    factor = 1.0e+6; //1.0e+6 meters
                    break;

                case Rhino.UnitSystem.Gigameters:
                    factor = 1.0e+9; //1.0e+9 meters
                    break;

                case Rhino.UnitSystem.Yards:
                    factor = 0.9144; //0.9144  meters, 36 inches
                    break;

                case Rhino.UnitSystem.PrinterPoints:
                    factor = 1 / 72; //1 / 72 inches, computer points
                    break;

                case Rhino.UnitSystem.PrinterPicas:
                    factor = 1 / 6; //1 / 6 inches, computer picas
                    break;

                case Rhino.UnitSystem.NauticalMiles:
                    factor = 1852; //mile 1852 meters
                    break;

                case Rhino.UnitSystem.AstronomicalUnits:
                    factor = 1.4959787e+11; // 1.4959787e+11
                    break;

                case Rhino.UnitSystem.LightYears:
                    factor = 9.46073e+15; //9.46073e+15 meters
                    break;

                case Rhino.UnitSystem.Parsecs:
                    factor = 3.08567758e+16; // 3.08567758e+16 meters
                    break;

                case Rhino.UnitSystem.CustomUnits:
                    throw new ArgumentOutOfRangeException("Unknown units");


                default:
                    return 1;

            }
            return 1 / factor;
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

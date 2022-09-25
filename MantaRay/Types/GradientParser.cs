using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    /// <summary>
    /// Original (C) Henning Larsen Architects 2019
    /// GPL License
    /// Author: Mathias Sønderskov
    /// https://github.com/HenningLarsenArchitects/Grasshopper_Doodles_Public
    /// </summary>
    class GradientParser
    {
        // Source primarily from : https://discourse.mcneel.com/t/get-values-from-a-gradient-component/108532/6

        readonly Grasshopper.GUI.Gradient.GH_Gradient Gradient = new Grasshopper.GUI.Gradient.GH_Gradient();
        public double? Max { get; set; } = null;
        public double? Min { get; set; } = null;
        public Color? AboveMax { get; set; }
        public Color? BelowMin { get; set; }
        //public bool Cap { get; set; }
        public bool Reverse { get; set; } = false;

        public GradientParser(GH_GradientControl gradientControl = null)
        {
            if (gradientControl != null)
            {
                Gradient = gradientControl.Gradient;

            }
            else
            {
                Gradient = GH_Gradient.Heat();

            }

            List<double> gripsParameters;
            List<Color> gripsColourLeft;
            List<Color> gripsColourRight;



            try
            {

                //this.Params.Input[0].Sources[0].Sources[0];


                if (gradientControl != null)
                {
                    GH_PersistentParam<GH_Number> param = (GH_PersistentParam<GH_Number>)gradientControl.Params.Input[2];
                    if (param != null && param.VolatileData.IsEmpty)
                    {
                        param.PersistentData.Append(new GH_Number(1.0));

                        param.ExpireSolution(true);
                        return;
                    }

                    GH_Structure<GH_Number> gradientFirstInput = (GH_Structure<GH_Number>)gradientControl.Params.Input[0].VolatileData;

                    Min = gradientFirstInput[0][0].Value;

                    GH_Structure<GH_Number> gradientSecondInput = (GH_Structure<GH_Number>)gradientControl.Params.Input[1].VolatileData;

                    Max = gradientSecondInput[0][0].Value;
                }

                else
                {
                    Min = 0;
                    Max = 1;
                }

                


                




                //GH_PersistentParam<GH_Number> minCast = (GH_PersistentParam<GH_Number>)gradientControl.Params.Input[0].VolatileData.AllData(false).First();

                //foreach (var item in minCast.VolatileData.AllData(false))
                //{
                //    bool casted = item.CastTo(out GH_Number number);
                //    if (casted) Min = number.Value;
                //}

                //GH_PersistentParam<GH_Number> maxCast = (GH_PersistentParam<GH_Number>)gradientControl.Params.Input[1].VolatileData.AllData(false).First();

                //foreach (var item in maxCast.VolatileData.AllData(false))
                //{
                //    bool casted = item.CastTo(out GH_Number number);
                //    if (casted) Max = number.Value;
                //}



                bool isLinear = Gradient.Linear;
                bool isLocked = Gradient.Locked;
                int gripCount = Gradient.GripCount;

                var parameters = new List<double>();
                var colourLeft = new List<Color>();
                var colourRight = new List<Color>();

                for (var i = 0; i < Gradient.GripCount; i++)
                {
                    parameters.Add(Gradient[i].Parameter);
                    colourLeft.Add(Gradient[i].ColourLeft);
                    colourRight.Add(Gradient[i].ColourRight);
                }
                gripsParameters = parameters;
                gripsColourLeft = colourLeft;
                gripsColourRight = colourRight;

            }
            catch
            {
            }

        }

        /// <summary>
        /// Use this method to get N colors
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public Color[] GetDefaultColors(int count = 50)
        {

            Color[] colors = new Color[count < 2 ? 2 : count];

            for (int i = 0; i < count; i++)
            {
                colors[i] = Gradient.ColourAt((double)i / (count - 1.0));
            }

            return colors;

        }

        public Color[] GetColors(IList<double> data)
        {
            //Rhino.RhinoApp.WriteLine($"{Min} to {Max}.. .and data is from {data.Min()} to {data.Max()}");
            //if (Cap)
            //    Rhino.RhinoApp.WriteLine("CAPPED");
            //else
            //    Rhino.RhinoApp.WriteLine("UNCAPPED");
            Color[] colors = new Color[data.Count];

            if (!Min.HasValue || !Max.HasValue)
                throw new Exception("Min or Max wasnt set for the GradientParser. Please do that before using me");



            for (int i = 0; i < data.Count; i++)
            {
                double lookupValue = (data[i] - Min.Value) / (Max.Value - Min.Value);
                if (Reverse)
                    lookupValue = 1 - lookupValue;


                colors[i] = Gradient.ColourAt(lookupValue);


                if (data[i] < Min)
                {

                    colors[i] = BelowMin ?? Gradient.ColourAt(Reverse ? 1 : 0);

                }

                if (data[i] > Max)
                {


                    colors[i] = AboveMax ?? Gradient.ColourAt(Reverse ? 0 : 1);


                }
            }



            return colors;

        }

    }
}

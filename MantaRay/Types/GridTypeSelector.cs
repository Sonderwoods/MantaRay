using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    class GridTypeSelector : IEnumerable<double>
    {
        public double? StepSize { get; set; }
        public int? Steps { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public List<double> ManuallySteps { get; set; }


        public override string ToString()
        {

            StringBuilder sb = new StringBuilder($"[InputSelector, {Min} to {Max}]:");


            int i = 0;
            foreach (var item in this)
            {
                if (i > 3)
                {
                    sb.Append($"\n... up to {Max}");
                    break;
                }
                sb.Append($"\n· {item:0.00}");
                i++;

            }

            return sb.ToString();


        }

        public GridTypeSelector(int steps, double? min = null, double? max = null)
        {
            Steps = steps;

            if (steps < 2)
                throw new Exception("Too few steps. Need at least 3");


            if (min.HasValue && max.HasValue && min > max)
            {
                throw new Exception("min>max in the input selector");
            }

            Min = min;
            Max = max;
        }

        public GridTypeSelector(List<double> manuallySteps)
        {
            manuallySteps.Sort();
            ManuallySteps = manuallySteps;

            Min = manuallySteps.Min();
            Max = manuallySteps.Max();
        }

        public GridTypeSelector(double stepSize, double? min = null, double? max = null)
        {
            StepSize = stepSize;

            if (min.HasValue && max.HasValue && min > max)
            {
                throw new Exception("min>max in the input selector");
            }

            Min = min;
            Max = max;



        }


        /// <summary>
        /// overrides the Min and Max value based on inputnumbers.min and max
        /// </summary>
        /// <param name="inputNumbers"></param>
        public void SetMinMax(IEnumerable<double> inputNumbers)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (double value in inputNumbers)
            {
                if (value < min)
                    min = value;

                if (value > max)
                    max = value;
            }
            Min = min;
            Max = max;
        }

        public IEnumerator<double> GetEnumerator()
        {

            if ((Min == null || Max == null) && ManuallySteps == null)
            {
                throw new Exception("Need to set min and max first. Parse your numbers into the SetMinMax method");
            }



            double span = Max.Value - Min.Value;

            if (ManuallySteps != null && ManuallySteps.Count > 0)
            {
                foreach (var item in ManuallySteps)
                {
                    yield return item;
                }

            }


            else if (Steps.HasValue && StepSize.HasValue)
            {
                throw new Exception("Both StepSize and Steps have values. I'm unsure what to use");
            }


            else if (Steps.HasValue)
            {

                for (int i = 0; i < Steps.Value; i++)
                {
                    yield return Min.Value + (double)i / (Steps.Value - 1.0) * span;
                }

            }


            else if (StepSize.HasValue)
            {
                double step = Min.Value;
                while (step <= Max)
                {
                    yield return step;
                    step += StepSize.Value;
                }
            }


            else
            {
                throw new Exception("StepSize and Steps were not set");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}

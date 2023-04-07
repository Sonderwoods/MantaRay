using MantaRay.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Radiance
{

    public class Material : RadianceObject
    {
        public string Definition { get; set; }


        public override string ToString() => Definition;
        public override string ModifierName => Definition.Split(' ')[0];
        public override string Type => Definition.Split(' ')[1];
        public override string Name => Definition.Split(' ')[2];

        public Material(string[] data) : base(data)
        {
            //IEnumerable<string> dataNoHeader = data.Skip(6);
            Definition = String.Join(" ", data.Take(3)) + "\n" + String.Join("\n", data.Skip(3).Take(3)) + "\n" + String.Join(" ", data.Skip(6));
        }

        public Material(string definition)
        {
            Definition = definition;
        }





        [Flags]
        public enum OpaqueMessages
        {
            None = 0,
            SpecularityAbove01 = 1,
            RoughnessAbove02 = 2,

        };

        [Flags]
        public enum GlassMessages
        {
            None = 0,
            TransmittanceAbove088 = 1,
            TransmittanceBelow030 = 2,

        };

        public static Material CreateOpaqueFromColor(string name, Color color, out OpaqueMessages messages, double roughness = 0.0, double specularity = 0.0)
        {
            name = StringHelper.ToSafeName(name);

            messages = OpaqueMessages.None;

            if (specularity >= 0.1)
                messages |= OpaqueMessages.SpecularityAbove01;

            if (roughness >= 0.2)
                messages |= OpaqueMessages.RoughnessAbove02;

            return new Material($"void plastic {name}\n" +
            $"0\n" +
            $"0\n" +
            $"5 {(color.R / 255.0).ToString(CultureInfo.InvariantCulture)} {(color.G / 255.0).ToString(CultureInfo.InvariantCulture)} {(color.B / 255.0).ToString(CultureInfo.InvariantCulture)} {specularity.ToString(CultureInfo.InvariantCulture)} {roughness.ToString(CultureInfo.InvariantCulture)}");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="reflection">0 to 1.0   .. if > 1, it will be divided by 100.0</param>
        /// <param name="messages"></param>
        /// <param name="roughness"></param>
        /// <param name="specularity"></param>
        /// <returns></returns>
        public static Material CreateOpaqueFromReflection(string name, double reflection, out OpaqueMessages messages, double roughness = 0.0, double specularity = 0.0)
        {

            name = StringHelper.ToSafeName(name);

            reflection = reflection > 1 ? (reflection / 100.0) : reflection;
            messages = OpaqueMessages.None;

            if (specularity >= 0.1)
                messages |= OpaqueMessages.SpecularityAbove01;

            if (roughness >= 0.2)
                messages |= OpaqueMessages.RoughnessAbove02;


            return new Material($"void plastic {name}\n" +
            $"0\n" +
            $"0\n" +
            $"5 {reflection.ToString(CultureInfo.InvariantCulture)} {reflection.ToString(CultureInfo.InvariantCulture)} {reflection.ToString(CultureInfo.InvariantCulture)} {specularity.ToString(CultureInfo.InvariantCulture)} {roughness.ToString(CultureInfo.InvariantCulture)}");
        }

        public static Material CreateOpaqueFromReflection(string name, int reflection, out OpaqueMessages messages, double roughness = 0.0, double specularity = 0.0)
        {
            name = StringHelper.ToSafeName(name);

            if (reflection > 255 || reflection < 0) throw new ArgumentOutOfRangeException(nameof(reflection));
            return CreateOpaqueFromReflection(name, (double)reflection, out messages, roughness, specularity);
        }

        public static Material CreateGlassFromColor(string name, Color color, out GlassMessages messages)
        {

            name = StringHelper.ToSafeName(name);

            messages = GlassMessages.None;

            double avgTransmittance = 255.0 / 100.0 * 0.3333 * (color.R * color.G * color.B);

            if (avgTransmittance > 0.88)
                messages |= GlassMessages.TransmittanceAbove088;
            if (avgTransmittance < 0.3)
                messages |= GlassMessages.TransmittanceBelow030;

            return new Material($"void glass {name}\n" +
                $"0\n" +
                $"0\n" +
                $"3 {TransmittanceToTransmissivity(color.R / 255.0 * 100.0).ToString(CultureInfo.InvariantCulture)} {TransmittanceToTransmissivity(color.G / 255.0 * 100.0).ToString(CultureInfo.InvariantCulture)} {TransmittanceToTransmissivity(color.B / 255.0 * 100.0).ToString(CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Transmittance needs to be a double from 0 to 1.0
        /// </summary>
        /// <param name="name"></param>
        /// <param name="transmittance"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static Material CreateGlassFromTransmittance(string name, double transmittance, out GlassMessages messages)
        {

            name = StringHelper.ToSafeName(name);

            transmittance = transmittance > 1 ? (0.01 * transmittance) : transmittance;

            messages = GlassMessages.None;

            return new Material($"void glass {name}\n" +
                $"0\n" +
            $"0\n" +
            $"3 {TransmittanceToTransmissivity(transmittance).ToString(CultureInfo.InvariantCulture)} {TransmittanceToTransmissivity(transmittance).ToString(CultureInfo.InvariantCulture)} {TransmittanceToTransmissivity(transmittance).ToString(CultureInfo.InvariantCulture)}");
        }
        public static Material CreateGlassFromTransmittance(string name, int transmittance, out GlassMessages messages)
        {
            name = StringHelper.ToSafeName(name);

            return CreateGlassFromTransmittance(name, (double)transmittance, out messages);
        }

        public static double TransmittanceToTransmissivity(double t)
        {
            return (Math.Sqrt(.8402528435 + .0072522239 * t * t) - .9166530661) / .0036261119 / t;
        }
    }
}

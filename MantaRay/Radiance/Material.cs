using System;
using System.Collections.Generic;
using System.Drawing;
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
            messages = OpaqueMessages.None;

            if (specularity >= 0.1)
                messages |= OpaqueMessages.SpecularityAbove01;

            if (roughness >= 0.2)
                messages |= OpaqueMessages.RoughnessAbove02;

            return new Material($"void plastic {name}\n" +
            $"0\n" +
            $"0\n" +
            $"5 {color.R / 255.0:0.000} {color.G / 255.0:0.000} {color.B / 255.0:0.000} {specularity:0.000} {roughness:0.000}");

        }

        public static Material CreateOpaqueFromReflection(string name, double reflection, out OpaqueMessages messages, double roughness = 0.0, double specularity = 0.0)
        {
            int _refl = (int)(reflection * 255.0);
            if (reflection > 255 || reflection < 0) throw new ArgumentOutOfRangeException(nameof(reflection));
            return CreateOpaqueFromColor(name, Color.FromArgb(_refl, _refl, _refl), out messages, roughness, specularity);
        }

        public static Material CreateOpaqueFromReflection(string name, int reflection, out OpaqueMessages messages, double roughness = 0.0, double specularity = 0.0)
        {
            if (reflection > 255 || reflection < 0) throw new ArgumentOutOfRangeException(nameof(reflection));
            return CreateOpaqueFromColor(name, Color.FromArgb(reflection, reflection, reflection), out messages, roughness, specularity);
        }

        public static Material CreateGlassFromColor(string name, Color color, out GlassMessages messages)
        {
            messages = GlassMessages.None;

            double avgTransmittance = 0.3333 * (color.R * color.G * color.B);

            if (avgTransmittance > 0.88)
                messages |= GlassMessages.TransmittanceAbove088;
            if (avgTransmittance < 0.3)
                messages |= GlassMessages.TransmittanceBelow030;

            return new Material($"void glass {name}\n" +
                $"0\n" +
                $"0\n" +
                $"3 {TransmittanceToTransmissivity(color.R):0.000} {TransmittanceToTransmissivity(color.G):0.000} {TransmittanceToTransmissivity(color.B):0.000}");
        }

        public static Material CreateGlassFromTransmittance(string name, double transmittance, out GlassMessages messages)
        {
            int _t = (int)(transmittance * 255.0);
            return CreateGlassFromColor(name, Color.FromArgb(_t, _t, _t), out messages);
        }

        public static Material CreateGlassFromTransmittance(string name, int transmittance, out GlassMessages messages)
        {

            return CreateGlassFromColor(name, Color.FromArgb(transmittance, transmittance, transmittance), out messages);
        }

        public static double TransmittanceToTransmissivity(double t)
        {
            return (Math.Sqrt(.8402528435 + .0072522239 * t * t) - .9166530661) / .0036261119 / t;
        }
    }
}

using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using MantaRay.Helpers;
using Rhino.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Types
{
    public class OverridableText : IGH_Goo
    {
        const string suffix = "\n\n# This is a MantaRay Dictionary\n# Globals will be updated in Execute component\n# Locals will not!\n# This tail is ignored by the Execute component";

        public OverridableText Parent { get; set; }
        public string RawData { get; set; }
        public Dictionary<string, string> Locals { get; set; }

        /// <summary>
        /// Gets the "live" value after globals being applied.
        /// </summary>
        public string Value
        {
            get
            {
                if (!string.IsNullOrEmpty(RawData) && Parent == null)
                    return RawData.ApplyGlobals(Locals);
                else if (string.IsNullOrEmpty(RawData) && Parent != null)
                    return Parent.Value.ApplyGlobals(Locals);
                else
                    return null;
            }

        }

        public bool IsValid => true;

        public string IsValidWhyNot => "";

        public string TypeName => "MantaRay.Text";

        public string TypeDescription => "Text that will have globals applied at runtime";

        public void CheckMissingKeys(List<string> missingKeys)
        {
            if (missingKeys != null)
            {
                if (Parent != null)
                {
                    Parent.Value.ApplyGlobals(Locals, missingKeys, 1);
                    //Parent.CheckMissingKeys(missingKeys);

                }
                if (RawData != null)
                {
                    RawData.ApplyGlobals(Locals, missingKeys, 1);
                }


            }
        }

        public OverridableText(string rawdata, Dictionary<string, string> locals = null, List<string> missingKeys = null)
        {
            RawData = rawdata;
            Locals = locals;

            //Just testing if any keys are missing and outputting error to missingKeys
            CheckMissingKeys(missingKeys);

        }

        public OverridableText(OverridableText parent, Dictionary<string, string> locals, List<string> missingKeys = null)
        {
            Parent = parent;
            Locals = locals;

            CheckMissingKeys(missingKeys);

        }

        public OverridableText()
        {

        }

        public override string ToString() => Value + suffix;

        public IGH_Goo Duplicate()
        {

            OverridableText t = new OverridableText(RawData, Locals);
            if (Parent != null)
            {
                t.Parent = (OverridableText)Parent.Duplicate();
            }
            

            return t;
        }

        public IGH_GooProxy EmitProxy()
        {
            return null;
        }

        public bool CastFrom(object source)
        {
            switch (source)
            {
                case OverridableText ot:
                    RawData = ot.RawData;
                    Locals = ot.Locals;
                    break;
                //case GH_String ghs:
                //    RawData = ghs.ToString();
                //    break;
                //case string s:
                //    RawData = s.ToString();
                //    break;
                default:
                    RawData = source.ToString();
                    break;
            }
            return true;
        }

        public bool CastTo<T>(out T target)
        {
            target = default;
            return false;
        }

        public object ScriptVariable()
        {
            return this;
        }

        public bool Write(GH_IWriter writer)
        {
            return true;
        }

        public bool Read(GH_IReader reader)
        {
            return true;
        }
    }
}

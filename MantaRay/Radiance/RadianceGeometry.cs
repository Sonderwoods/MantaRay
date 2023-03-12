﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MantaRay.Radiance.HeadsUpDisplay;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Radiance
{

    public abstract class RadianceGeometry : RadianceObject, IHasPreview
    {
        
        public Rhino.Display.DisplayMaterial Material { get; set; } = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Gray);

        public RadianceGeometry(string[] data) : base(data)
        {

        }

        public RadianceGeometry()
        {

        }





        public abstract BoundingBox? GetBoundingBox();
    
        public virtual bool HasPreview() => true;

        public virtual string GetName() => Name;
        public virtual string GetDescription()
        {
            if (Modifier != null && Modifier is Material m)
            {
                return m.Definition ?? "No Modifiers found";
            }
             return "No Modifiers found";
        }
        public abstract void DrawPreview(IGH_PreviewArgs args, DisplayMaterial material, double? transparency = null);
        public abstract void DrawWires(IGH_PreviewArgs args, int thickness = 1);
        public abstract IEnumerable<GeometryBase> GetGeometry();
    }
}
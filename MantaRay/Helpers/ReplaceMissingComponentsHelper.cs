using Grasshopper.Kernel.Undo.Actions;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rhino.UI.Controls;
using Grasshopper.Kernel.Special;

namespace MantaRay.Helpers
{
    public static class ReplaceMissingComponentsHelper
    {

        /// <summary>
        /// Checks if the component contains obsolete and if a newer version exists.
        /// </summary>
        /// <param name="component"></param>
        public static void FixAbsolete(IGH_DocumentObject component)
        {
            List<Type> newTypes = GetSimilarObsoleteTypes(component);
            if (newTypes.Count() > 0)
            {
                Type newType = GetNonObsoleteType(component);
                if (newType != null)
                {
                    SchedulePlaceNewComponent(component, newTypes);
                }
            }
                


        }

        /// <summary>
        /// Get all types that has similar name as <paramref name="component"/> where _OBSOLETE is removed.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static List<Type> GetSimilarObsoleteTypes(IGH_DocumentObject component)
        {
            List<Type> types = new List<Type>();
            string className = component.GetType().Name;
            if (className != null && className.ToUpper().Contains("OBSOLETE"))
            {
                foreach (Type type in component.GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(GH_Template)) && type != component.GetType()))
                {
                    if (type.Name.ToUpper() == className.ToUpper().Replace("_OBSOLETE", ""))
                    {
                        types.Add(type);

                    }
                }

            }
            return types;

        }

        /// <summary>
        /// Finds the "NON OBSOLETE" version of the component.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Type GetNonObsoleteType(IGH_DocumentObject component)
        {
            var types = GetSimilarObsoleteTypes(component);

            foreach (Type type in types)
            {
                if(!type.Name.ToUpper().Contains("OBSOLETE"))
                {
                    return type;
                }
            }
            return null;
        }

        public static bool IsInAGroup(IGH_DocumentObject component, GH_Document doc, string groupName = null)
        {
            if (component == null || doc == null)
            {
                throw new Exception("Tried to call IsInAGroup without linking component and/or doc");
            }

            IEnumerable<GH_Group> objects = doc.Objects.OfType<GH_Group>();

            if (!string.IsNullOrEmpty(groupName))
            {
                objects = objects.Where(g => string.Equals(groupName, g.NickName, StringComparison.InvariantCultureIgnoreCase));
            }

            return objects.Any(g => g.ObjectsRecursive().Contains(component));


        }

        /// <summary>
        /// Will place a new component and is called in two scenarios:
        /// <para>1) If the existing component has wrongly setup params - will reinsert the same type</para>
        /// 2) If the existing component is OBSOLETE and the namespace contains a newer non-obsolete of the same type (<paramref name="newType"/> != null)
        /// </summary>
        /// <param name="component"></param>
        /// <param name="similarTypes"></param>
        /// <param name="newType"></param>
        public static void SchedulePlaceNewComponent(IGH_DocumentObject component = null, IEnumerable<Type> similarTypes = null, Type newType = null)
        {
            if (component == null)
                return;

            GH_Document doc = component.OnPingDocument();

            //GH_Document doc = Grasshopper.Instances.ActiveCanvas.Document;

            //MethodInfo method = typeof(Queryable).GetMethod("OfType");
            //MethodInfo generic = method.MakeGenericMethod(new Type[] { type });

            ////https://stackoverflow.com/questions/3669760/iqueryable-oftypet-where-t-is-a-runtime-type
            //IEnumerable<IGH_Component> objectsOfSameType = ((IEnumerable<object>)generic.Invoke
            //      (null, new object[] { doc.Objects })).OfType<IGH_Component>();


            int tol = 35;

            newType = newType ?? GetNonObsoleteType(component) ?? component.GetType();

            similarTypes = similarTypes ?? GetSimilarObsoleteTypes(component);


            var objectsOfSameType = doc.Objects.Where(c => (c.ComponentGuid == component.ComponentGuid || similarTypes.Contains(c.GetType())) && !object.ReferenceEquals(c, component));

            foreach (IGH_Component obj in objectsOfSameType)
            {

                if (Math.Abs(obj.Attributes.Pivot.X - component.Attributes.Pivot.X) < tol &&
                    Math.Abs(obj.Attributes.Pivot.Y - component.Attributes.Pivot.Y) < tol)
                {
                    return;

                }
            }

            if (IsInAGroup(component, doc, "Outdated"))
                return;

            

            Task.Factory.StartNew(() =>
            {
                var record = new GH_UndoRecord
                {
                    Name = $"Create New Duplicate of {component.NickName}"
                };

                PlaceNewComponent(doc, (IGH_Component)component, record, newType);

                GroupAComponent(doc, (IGH_Component)component, "Outdated", Color.Red, record);

                doc.UndoUtil.RecordEvent(record);

            });


        }

        /// <summary>
        /// Places a component in the document and uses a undorecord. Does NOT record the record. Must be done manually with <see cref="GH_Document.UndoUtil"/>
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="component"></param>
        public static void PlaceNewComponent(GH_Document doc, IGH_Component component, GH_UndoRecord record = null, Type replacementType = null)
        {
            var pivot = component.Attributes.Pivot;

            //* https://www.grasshopper3d.com/forum/topics/ins-and-outs-of-undo
            //* 1. declare a new GH_UndoRecord, give it a name
            //* 2. for all major changes to objects (ObjectsAdded, Wires, other generic changes) declare a new corresponding GH_AddObjectAction or GH_WireAction or whatever
            //* 3. Add that Action to the GH_UndoRecord declared earlier
            //* 4. THEN and only then actually make the change (adding the object to the doc, changing the wires, etc)
            //* 5. after all the actions have been recorded, pass the UndoRecord to GH_Document.UndoUtil.RecordEvent. 

            record = record ?? new GH_UndoRecord
            {
                Name = $"Create New Duplicate of {component.NickName}"
            };

            IGH_Component newComponent = (IGH_Component)Activator.CreateInstance(replacementType ?? component.GetType());

            GH_AddObjectAction action = new GH_AddObjectAction(newComponent);
            record.AddAction(action);

            newComponent.CreateAttributes();
            newComponent.Attributes.Pivot = new PointF(pivot.X + 30, pivot.Y + 30);

            newComponent.Attributes.ExpireLayout();
            newComponent.Attributes.PerformLayout();


            doc.AddObject(newComponent, false);

            Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(newComponent, GH_Arrange.MoveToFront);

            GroupAComponent(doc, component, "Outdated", Color.Red, record);
            

        }


        /// <summary>
        /// Puts a single grasshopper component into a group with color and title. Sets an undo record or uses the input one.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="component"></param>
        /// <param name="title"></param>
        /// <param name="color"></param>
        /// <param name="record"></param>
        public static void GroupAComponent(GH_Document doc, IGH_Component component, string title = "", Color color = default, GH_UndoRecord record = null)
        {
            if (component == null || doc == null)
                return;

            bool newRecord = false;

            if (record == null)
            {
                record = new GH_UndoRecord
                {
                    Name = $"Grouping {component.NickName} with \"{title}\""
                };
                newRecord = true;
            }


            Guid guid = doc.Objects.Where(o => object.ReferenceEquals(o, component)).FirstOrDefault().InstanceGuid;



            Grasshopper.Kernel.Special.GH_Group newGroup = new Grasshopper.Kernel.Special.GH_Group()
            {
                Colour = color,
                NickName = title,
                Border = Grasshopper.Kernel.Special.GH_GroupBorder.Rectangles,

            };

            newGroup.AddObject(guid);

            GH_AddObjectAction action2 = new GH_AddObjectAction(newGroup);
            record.AddAction(action2);

            doc.AddObject(newGroup, false);

            if (newRecord)
                doc.UndoUtil.RecordEvent(record);


        }
    }
}

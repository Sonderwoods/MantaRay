using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using Rhino.DocObjects;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;

namespace MantaRay
{
    /// <summary>
    /// DataAccessHelper for GH. Original from Arend van Waart  arend@studioavw.nl
    /// 
    /// Boosted errormessages, allowing for params[] inputs for renamed params, creating duplicate components as part of MantaRay
    /// </summary>
    static class GH_AccessHelper
    {

        // Source: https://github.com/arendvw/clipper/tree/master/ClipperComponents/Helpers
        // Author: Arend van Waart arend@studioavw.nl

        const string msg = "\nThis might be because your component is outdated. Try to drag a new component of this type to the canvas and see if it helps :-)";


        /// <summary>
        /// Iterates over an Enum type to add the named values to the integer param
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cfParam"></param>
        internal static void AddEnumOptionsToParam<T>(Param_Integer cfParam)
        {
            foreach (int cfType in Enum.GetValues(typeof(T)))
            {
                var name = Enum.GetName(typeof(T), cfType);
                cfParam.AddNamedValue(name, cfType);
            }
        }

        //public static T Fetch<T>(this IGH_DataAccess da, int position)
        //{
        //    return Fetch<T>(da, null, position);
        //}

        //public static T Fetch<T>(this IGH_DataAccess da, params string[] names)
        //{
        //    return Fetch<T>(da, null, names);
        //}
        //public static List<T> FetchList<T>(this IGH_DataAccess da, int position)
        //{
        //    return FetchList<T>(da, null, position);
        //}
        //public static List<T> FetchList<T>(this IGH_DataAccess da, params string[] names)
        //{
        //    return FetchList<T>(da, null, names);
        //}
        //public static GH_Structure<T> FetchTree<T>(this IGH_DataAccess da, int position) where T : IGH_Goo
        //{
        //    return FetchTree<T>(da, null, position);
        //}
        //public static GH_Structure<T> FetchTree<T>(this IGH_DataAccess da, string name) where T : IGH_Goo
        //{
        //    return FetchTree<T>(da, null, name);
        //}

        /// <summary>
        /// Fetch data at index position
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="da"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static T Fetch<T>(this IGH_DataAccess da, IGH_DocumentObject obj, int position)
        {

            var temp = default(T);
            try
            {
                da.GetData(position, ref temp);

            }
            catch (System.IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent();
                throw new IndexOutOfRangeException($"Input parameter not found at position {position}" + msg, e);
            }
            catch (System.InvalidOperationException e)
            {
                SchedulePlaceNewComponent();
                throw new InvalidOperationException($"item instead of list!?: {position}" + msg, e);
            }
            return temp;
        }



        public static T Fetch<T>(this IGH_DataAccess da, IGH_DocumentObject obj, params string[] names)
        {
            var temp = default(T);

            foreach (var name in names)
            {
                try
                {
                    da.GetData(name, ref temp);
                    return temp;
                }
                catch (System.IndexOutOfRangeException)
                {
                    continue;
                }
                catch (System.InvalidOperationException e)
                {
                    SchedulePlaceNewComponent(obj);
                    throw new InvalidOperationException($"item instead of list!?: {name}" + msg, e);
                }
            }
            SchedulePlaceNewComponent(obj);
            throw new IndexOutOfRangeException($"Input parameter not found: \"{string.Join("\", \"", names)}\"\n+{msg}");
        }
        /// <summary>
        /// Fetch data with name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="da"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T Fetch<T>(this IGH_DataAccess da, IGH_DocumentObject obj, string name)
        {
            var temp = default(T);
            try
            {
                da.GetData(name, ref temp);

            }
            catch (System.IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found: {name}" + msg, e);
            }
            catch (System.InvalidOperationException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new InvalidOperationException($"item instead of list!?: {name}" + msg, e);
            }
            return temp;
        }





        /// <summary>
        /// Fetch data list with position
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="da"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static List<T> FetchList<T>(this IGH_DataAccess da, IGH_DocumentObject obj, int position)
        {
            var temp = new List<T>();
            try
            {
                da.GetDataList(position, temp);

            }
            catch (System.IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found at position {position}" + msg, e);
            }
            catch (System.InvalidOperationException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new InvalidOperationException($"item instead of list!?: {position}" + msg, e);
            }
            return temp;
        }



        /// <summary>
        /// Fetch data list with name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="da"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static List<T> FetchList<T>(this IGH_DataAccess da, IGH_DocumentObject obj, params string[] names)
        {
            var temp = new List<T>();

            foreach (var name in names)
            {
                try
                {
                    da.GetDataList(name, temp);
                    return temp;
                }
                catch (System.IndexOutOfRangeException)
                {
                    continue;
                }
                catch (System.InvalidOperationException e)
                {
                    SchedulePlaceNewComponent(obj);
                    throw new InvalidOperationException($"item instead of list!?: {name}" + msg, e);
                }
            }
            SchedulePlaceNewComponent(obj);
            throw new IndexOutOfRangeException($"Input parameter not found: \"{string.Join("\", \"", names)}\"\n+{msg}");
        }




        /// <summary>
        /// Fetch structure with position
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="da"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static GH_Structure<T> FetchTree<T>(this IGH_DataAccess da, IGH_DocumentObject obj, int position) where T : IGH_Goo
        {
            GH_Structure<T> temp = default(GH_Structure<T>);
            try
            {

                da.GetDataTree(position, out temp);
            }
            catch (System.IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found at position {position}" + msg, e);
            }
            catch (System.InvalidOperationException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new InvalidOperationException($"item instead of list!?: {position}" + msg, e);
            }
            return temp;
        }



        /// <summary>
        /// Fetch structure with name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="da"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GH_Structure<T> FetchTree<T>(this IGH_DataAccess da, IGH_DocumentObject obj, string name) where T : IGH_Goo
        {

            GH_Structure<T> temp = default(GH_Structure<T>);
            try
            {
                da.GetDataTree(name, out temp);

            }
            catch (System.IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found: {name}" + msg, e);
            }
            catch (System.InvalidOperationException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new InvalidOperationException($"item instead of list!?: {name}" + msg, e);
            }
            return temp;
        }

        public static void SchedulePlaceNewComponent(IGH_DocumentObject component = null)
        {
            if (component == null)
                return;

            component.OnPingDocument();

            GH_Document doc = Grasshopper.Instances.ActiveCanvas.Document;

            //MethodInfo method = typeof(Queryable).GetMethod("OfType");
            //MethodInfo generic = method.MakeGenericMethod(new Type[] { type });

            ////https://stackoverflow.com/questions/3669760/iqueryable-oftypet-where-t-is-a-runtime-type
            //IEnumerable<IGH_Component> objectsOfSameType = ((IEnumerable<object>)generic.Invoke
            //      (null, new object[] { doc.Objects })).OfType<IGH_Component>();

            
            int tol = 35;

            bool skipCreation = false;

            Type newType = null;
            string className = component.GetType().Name;
            if(className.Contains("OBSOLETE"))
            {
                foreach (Type type in component.GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(GH_Template)) && type != component.GetType()))
                {
                    if(type.Name == className.Replace("_OBSOLETE",""))
                    {
                        skipCreation = true;
                    }
                }
                
            }

            var objectsOfSameType = doc.Objects.Where(c => (c.ComponentGuid == component.ComponentGuid || c.GetType() == newType) && !object.ReferenceEquals(c, component));
            foreach (IGH_Component obj in objectsOfSameType)
            {

                if (Math.Abs(obj.Attributes.Pivot.X - component.Attributes.Pivot.X) < tol &&
                    Math.Abs(obj.Attributes.Pivot.Y - component.Attributes.Pivot.Y) < tol)
                {
                    return;

                }
            }

            Task.Factory.StartNew(() => PlaceNewComponent(doc, (IGH_Component)component, skipCreation));


        }

        public static void PlaceNewComponent(GH_Document doc, IGH_Component component, bool skipCreation = false)
        {
            var pivot = component.Attributes.Pivot;

            //* https://www.grasshopper3d.com/forum/topics/ins-and-outs-of-undo
            //* 1. declare a new GH_UndoRecord, give it a name
            //* 2. for all major changes to objects (ObjectsAdded, Wires, other generic changes) declare a new corresponding GH_AddObjectAction or GH_WireAction or whatever
            //* 3. Add that Action to the GH_UndoRecord declared earlier
            //* 4. THEN and only then actually make the change (adding the object to the doc, changing the wires, etc)
            //* 5. after all the actions have been recorded, pass the UndoRecord to GH_Document.UndoUtil.RecordEvent. 

            GH_UndoRecord record = new GH_UndoRecord();
            record.Name = $"Create New Duplicate of {component.NickName}";


            if(!skipCreation)
            {
                IGH_Component newComponent = (IGH_Component)Activator.CreateInstance(component.GetType());

                GH_AddObjectAction action = new GH_AddObjectAction(newComponent);
                record.AddAction(action);

                newComponent.CreateAttributes();
                newComponent.Attributes.Pivot = new PointF(pivot.X + 30, pivot.Y + 30);

                newComponent.Attributes.ExpireLayout();
                newComponent.Attributes.PerformLayout();


                doc.AddObject(newComponent, false);

                Grasshopper.Instances.ActiveCanvas.Document.ArrangeObject(newComponent, GH_Arrange.MoveToFront);
            }
            //Reflection magics!
            


            Guid guid = doc.Objects.Where(o => object.ReferenceEquals(o, component)).FirstOrDefault().InstanceGuid;

            

            Grasshopper.Kernel.Special.GH_Group newGroup = new Grasshopper.Kernel.Special.GH_Group()
            {
                Colour = Color.Red,
                Name = "Outdated",
                Border = Grasshopper.Kernel.Special.GH_GroupBorder.Rectangles,

            };

            newGroup.AddObject(guid);

            GH_AddObjectAction action2 = new GH_AddObjectAction(newGroup);
            record.AddAction(action2);

            doc.AddObject(newGroup, false);


            doc.UndoUtil.RecordEvent(record);
        }
    }
}
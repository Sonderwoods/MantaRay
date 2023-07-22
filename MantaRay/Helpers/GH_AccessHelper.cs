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
using static MantaRay.Helpers.ReplaceMissingComponentsHelper;

namespace MantaRay.Helpers
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
            catch (IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found at position {position}" + msg, e);
            }
            catch (InvalidOperationException e)
            {
                SchedulePlaceNewComponent(obj);
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
                catch (IndexOutOfRangeException)
                {
                    continue;
                }
                catch (InvalidOperationException e)
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
            catch (IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found: {name}" + msg, e);
            }
            catch (InvalidOperationException e)
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
            catch (IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found at position {position}" + msg, e);
            }
            catch (InvalidOperationException e)
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
                catch (IndexOutOfRangeException)
                {
                    continue;
                }
                catch (InvalidOperationException e)
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
            GH_Structure<T> temp = default;
            try
            {

                da.GetDataTree(position, out temp);
            }
            catch (IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found at position {position}" + msg, e);
            }
            catch (InvalidOperationException e)
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

            GH_Structure<T> temp = default;
            try
            {
                da.GetDataTree(name, out temp);

            }
            catch (IndexOutOfRangeException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new IndexOutOfRangeException($"Input parameter not found: {name}" + msg, e);
            }
            catch (InvalidOperationException e)
            {
                SchedulePlaceNewComponent(obj);
                throw new InvalidOperationException($"item instead of list!?: {name}" + msg, e);
            }
            return temp;
        }

    }
}
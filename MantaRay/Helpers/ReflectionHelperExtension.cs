//using Grasshopper.Kernel;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;

//namespace GrasshopperRadianceLinuxConnector
//{
//    public static class ReflectionHelperExtensionMethods
//    {

//        // Not in use, we could override the ProcessorTime getter instead :-)
        
//        /// <summary>
//        /// Sets private 
//        /// https://stackoverflow.com/a/1565766/13680266
//        /// </summary>
//        /// <param name="member"></param>
//        /// <param name="propName"></param>
//        /// <param name="runtime">runtime in ms</param>
//        public static void SetPrivateRuntimePropertyValue(this GH_Component member, int runtime)
//        {
//            TimeSpan ts = new TimeSpan(0, 0, 0, 0, runtime);
//            PropertyInfo propertyInfo = typeof(GH_Component).GetProperty("_profiledSpan");
//            FieldInfo fieldInfo = typeof(GH_Component).GetField("_profiledSpan");
//            if (fieldInfo == null) return;
//            fieldInfo.SetValue(member, ts);


//            if (propertyInfo == null) return;
//            propertyInfo.SetValue(member, ts);
//            member.OnDisplayExpired(true);

//        }
//    }
//}

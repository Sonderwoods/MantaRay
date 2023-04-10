using System;
using System.Collections.Generic;

using System.Drawing;
using ClipperLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using MantaRay.Components;
using Rhino.Geometry;
using MantaRay.Helpers;

namespace MantaRay.Components
{
    public class GH_ConvertPaths : GH_Template, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the GH_ToLinux class.
        /// </summary>
        public GH_ConvertPaths()
          : base("ConvertPaths", "ConvertPaths",
              "converts a windows path to linux path",
              "0 Setup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("I", "I", "Input linux/windows path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Linux", "L", "Linux", GH_ParamAccess.item);
            pManager.AddTextParameter("Windows", "W", "Windows", GH_ParamAccess.item);
            //pManager.AddTextParameter("Sftp", "F", "Sftp", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = DA.Fetch<string>(this, 0);

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());
            if (sshHelper == null || sshHelper.CheckConnection() != SSH_Helper.ConnectionDetails.Connected)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No connection");
                return;
            }

 
            DA.SetData(0, path.ToLinuxPath());
            DA.SetData(1, path.ToWindowsPath());

            if (Params.Output.Count == 3)
            {
                DA.SetData(2, path.ToSftpPath());
            }

        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Output && index == 2 && Params.Output.Count == 2;
        }

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Output && index == 2;
        }

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {

            var param = new Param_String { NickName = "-" };
            return param;


        }

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {

            if (Params.Output.Count == 3)
            {
                var param = Params.Output[2];
                if (param.NickName == "-")
                {
                    param.NickName = "F";
                    param.Name = "Sftp";
                    param.Access = GH_ParamAccess.item;
                }
            }

        }

        protected override Bitmap Icon => Resources.Resources.Ra_Paths_Icon;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("AF816E54-D307-4E15-9FDD-3DD9A0DC61EE"); }
        }
    }
}
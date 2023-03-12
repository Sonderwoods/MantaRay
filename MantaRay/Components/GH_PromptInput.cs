using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;
using MantaRay.Setup;
using Rhino.Geometry;

namespace MantaRay.Components
{
    public class GH_PromptInput : GH_Template
    {
        string _p = "";

        /// <summary>
        /// Initializes a new instance of the GH_PromptInput class.
        /// </summary>
        public GH_PromptInput()
          : base("PromptInput", "Input",
              "Promt an input and saves that string. Usefull for passwords etc.",
              "0 Setup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Prompt", "Prompt", "text to popup in the prompt", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Ask", "Ask", "Ask", GH_ParamAccess.item);
            pManager[pManager.AddBooleanParameter("Hide", "Hide", "Hide, default is true", GH_ParamAccess.item, true)].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Output", "output string from the prompt", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if(DA.Fetch<bool>(this, 1) && GetCredentials(DA.Fetch<string>(this, 0), out string p, DA.Fetch<bool>(this, 2)))
                _p = p;

            DA.SetData(0, _p);
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F53F981A-3CD9-4989-9F55-A5852599CD45"); }
        }



        private bool GetCredentials(string inp, out string p, bool hide)
        {

            var foreColor = Color.FromArgb(88, 100, 84);
            var backColor = Color.FromArgb(148, 180, 140);
            var background = Color.FromArgb(255, 195, 195, 195);

            Font redFont = new Font("Arial", 18.0f,
                        FontStyle.Bold);

            Font font = new Font("Arial", 10.0f,
                        FontStyle.Bold);

            Font smallFont = new Font("Arial", 8.0f,
                        FontStyle.Bold);

            Form prompt = new Form()
            {
                Width = 460,
                Height = 370,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "MantaRay: Inputs needed",
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = background,
                ForeColor = Color.FromArgb(255, 30, 30, 30),
                Font = font

            };


            Label label = new Label()
            {
                Left = 50,
                Top = 45,
                Width = 340,
                Height = 28,
                Text = inp + "\nThe input will NOT be saved in your grasshopper file in case you close it"
            };

            TextBox passwordTextBox = new TextBox()
            {
                Left = 50,
                Top = 125,
                Width = 340,
                Height = 28,
                Text = "",
                ForeColor = foreColor,
                Font = redFont,
                BackColor = backColor,
                Margin = new Padding(2),

            };

            if (hide)
                passwordTextBox.PasswordChar = '*';


            Button connectButton = new Button() { Text = "Submit", Left = 50, Width = 120, Top = 190, Height = 40, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 270, Width = 120, Top = 190, Height = 40, DialogResult = DialogResult.Cancel };


            Label label2 = new Label()
            {
                Font = smallFont,
                Left = 50,
                Top = 270,
                Width = 340,
                Height = 60,
                Text = $"Part of the {ConstantsHelper.ProjectName} plugin\n" +
                "(C) Mathias Sønderskov Schaltz 2022"
            };
            prompt.Controls.AddRange(new Control[] { label, passwordTextBox, connectButton, cancel, label2 });

            prompt.AcceptButton = connectButton;


            DialogResult result = prompt.ShowDialog();

            if (result == DialogResult.OK)
            {
                p = passwordTextBox.Text;

                return true;
            }
            else
            {
                p = null;

                return false;

            }
        }
    }
}
using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using MantaRay.Components.VM;
using Rhino.UI;
using System.Text;
using Renci.SshNet;
using System.Linq;
using System.Diagnostics;
using MantaRay.Components.Controls;
using MantaRay.Helpers;
using System.Data;

namespace MantaRay.Components.Views
{
    public class MyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.Convert.ToString(value) + " (converted)";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.Convert.ToString(value) + " (converted back)";
        }
    }


    public partial class LoginView : Dialog<Rhino.Commands.Result>
    {

        protected TextBox IpTextBox { get; set; }
        protected TextBox UserNameTextBox { get; set; }
        protected PasswordBox PasswordBox { get; set; }
        protected Label StatusLabel { get; set; }

        protected RoundedButton CButton { get; set; }

        protected StackLayout TestStack { get; set; }



        protected LoginVM ViewModel => DataContext as LoginVM;



        public LoginView(LoginVM vm)
        {

            DataContext = vm;



            XamlReader.Load(this);

            CButton.ToggleMode = RoundedButton.ToggleModes.Press;


            SetupStyles();

            SetupBinding();

            UpdateColors();

            PasswordBox.Focus();

            PasswordBox.KeyDown += PasswordBox_KeyDown;

            Invalidate();

            IpTextBox.Text = $"{vm.Ip}:{vm.Port}";

            //PasswordBox.KeyDown += (s, e) => TestButton_Click(s, e);
            //IpTextBox.KeyDown += (s, e) => TestButton_Click(s, e);
            //PasswordBox.KeyDown += (s, e) => TestButton_Click(s, e);

            vm.ConnectionStatus = "";


        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
            {
                TestButton_Click(sender, e);
                //SaveAndClose();
            }
        }

        private void SetupStyles()
        {
            //Eto.Style.Add<Eto.Wpf.Forms.Controls.ButtonHandler>(null, h =>
            //{
            //    h.Control.BorderThickness = new System.Windows.Thickness(3);
            //    h.Control.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 100, 30));


            //});
        }

        private void SetupBinding()
        {
            AbortButton.Click += AbortButton_Click;
            DefaultButton.Click += DefaultButton_Click;

            CButton.Click += TestButton_Click;

        }

        private void DefaultButton_Click(object sender, EventArgs e)
        {
            SaveAndClose();
        }


        /// <summary>
        /// updates VM, saves and closes
        /// </summary>
        private void SaveAndClose()
        {
            UpdateVM();
            Close(Rhino.Commands.Result.Success);
        }

        /// <summary>
        /// Sets the ip etc into the viewmodel
        /// </summary>
        private void UpdateVM()
        {
            ViewModel.Ip = IpTextBox.Text.Split(':').First();
            int port;
            if (IpTextBox.Text.Contains(":"))
            {
                if (!int.TryParse(IpTextBox.Text.Split(':').Last(), out port)) port = 22;
            }
            else
            {
                port = 22;
            }
            ViewModel.Port = port;
            ViewModel.Username = UserNameTextBox.Text;
            ViewModel.Password = PasswordBox.Text;
        }

        private void AbortButton_Click(object sender, EventArgs e)
        {
            Close(Rhino.Commands.Result.Cancel);

        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            UpdateVM();
            ViewModel.Connect(new Action(() => Close(result: Rhino.Commands.Result.Success)));


            // TODO: Need to look into PropertyNotified  and more async stuff + ui updates

        }

        protected void HandleIpChanged(object sender, EventArgs e)
        {
            UpdateColors();
            ChangeDetails();

        }

        protected void DetailsChanged(object sender, EventArgs e)
        {
            ChangeDetails();
        }


        private void ChangeDetails()
        {
            ViewModel.CanTestNewConnection = true;
        }


        protected void UpdateColors()
        {
            if (IpTextBox is null) { return; }

            bool IsLocalIp = ViewModel.LocalIp(IpTextBox.Text);

            Color foregroundColor = IsLocalIp ? Color.FromArgb(88, 100, 84) : Color.FromArgb(128, 96, 59);
            Color backColor = IsLocalIp ? Color.FromArgb(148, 180, 140) : Color.FromArgb(250, 205, 170);
            Color backGroundColor = IsLocalIp ? Color.FromArgb(195, 195, 195) : Color.FromArgb(201, 195, 157);

            IpTextBox.BackgroundColor = backColor;
            IpTextBox.TextColor = foregroundColor;

            UserNameTextBox.BackgroundColor = backColor;
            UserNameTextBox.TextColor = foregroundColor;

            PasswordBox.BackgroundColor = backColor;
            PasswordBox.TextColor = foregroundColor;

            BackgroundColor = backGroundColor;

        }


    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AxisDemoSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FillinXaddrLoginPasswordDialogContent : Page
    {
        public FillinXaddrLoginPasswordDialogContent()
        {
            this.InitializeComponent();
        }

        public string OnvifDeviceXaddr { get { return CameraIPv4AddresseTextBoxName.Text as string; } }
        public string OnvifDeviceUserName { get { return CameraUserNameTextBoxName.Text as string; } }
        public string OnvifDevicePassword { get {  return CameraPasswordTextBoxName.Text as string; } }

        public Visibility DeviceAddressErrorIconVisibility
        {
            get { return CameraAddressErrorIconName.Visibility; }
            set { CameraAddressErrorIconName.Visibility = value; }
        }

        private void CameraUserNameTextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(CameraUserNameErrorIconName.Visibility == Visibility.Visible)
            {
                CameraUserNameErrorIconName.Visibility = Visibility.Collapsed;
            }
        }

        private void CameraIPv4AddresseTextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(CameraAddressErrorIconName.Visibility == Visibility.Visible)
            {
                CameraAddressErrorIconName.Visibility= Visibility.Collapsed;
            }
        }

        private void CameraPasswordTextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(CameraPasswordErrorIconName.Visibility == Visibility.Visible)
            {
                CameraPasswordErrorIconName.Visibility = Visibility.Collapsed;
            }
        }
    }
}

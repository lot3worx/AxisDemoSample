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
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            

        }


        private Uri devicemanagementXaddr;

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            OnvifEventTests tests = new OnvifEventTests();

            tests.InitializeAsync();
        }

        private async void AddParametersButtonName_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = MainWindowGridName.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Fill In Onvif Device Service Credentials";
            dialog.PrimaryButtonText = "OK";
            dialog.SecondaryButtonText = "Cancel";
            dialog.CloseButtonText = null;
            dialog.DefaultButton = ContentDialogButton.Primary;
            FillinXaddrLoginPasswordDialogContent cn = new FillinXaddrLoginPasswordDialogContent();
            dialog.Content = cn;
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;
            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                FillinXaddrLoginPasswordDialogContent fc = dialog.Content as FillinXaddrLoginPasswordDialogContent;
                
                
                OnvifEventTests tests = new OnvifEventTests(fc.OnvifDeviceXaddr, fc.OnvifDeviceUserName, fc.OnvifDevicePassword);
                tests.InitializeAsync();
            }
            else
            {
                return;
            }
        }

        private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if(sender.Content is FillinXaddrLoginPasswordDialogContent)
            {
                FillinXaddrLoginPasswordDialogContent fc = sender.Content as FillinXaddrLoginPasswordDialogContent;
                Uri xadder = null;
                bool uriSuccess = Uri.TryCreate(fc.OnvifDeviceXaddr, UriKind.Absolute, out xadder);

                if(uriSuccess)
                {

                }
                else
                {
                    {
                        args.Cancel = true;
                        fc.DeviceAddressErrorIconVisibility = Visibility.Visible;
                        return;
                    }
                }

            }
            
        }
    }
}

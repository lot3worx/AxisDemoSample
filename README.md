# AxisDemoSample

Prerequsites

Windows 11 version 22621 or later for target test machine

Run in DEbug mode since the assets are not fully specified and it should NOT run as an installed app by itself.

It is specified in the project and should just happen when you build but ensure that:

You compile in the Microsoft Nuget Service Model stack including items such as system.servicemodel.* Also ensure that you have the latest version since if you load them yourself you need to check for the updates and then upgrade.

You also need to check that the official Onvif WSDL for device management and eventss are loaded as connected WCF services as per usual. More precisely, generated code
for

https://www.onvif.org/ver10/device/wsdl/devicemgmt.wsdl

and

https://www.onvif.org/ver10/events/wsdl/event.wsdl

Options in the service add dialog is to generate message contracts for all, public access to classes, asychnronouse function message only (no synchronous functions). Those of you who use connected service
from the ONvif site will be familiar with this.

The project runs as .net 6 C#

In Class OnvifEventTests you need to declare the username account and password of the Onvif device you are testing as well as the Onvif Xaddr for the
device service. See the declarations in the class for strings

        private string xaddr = "http://192.168.0.13/onvif/device_service";
        
        private string loginName = "UserName";
        private string password = "The Password";

Startup:

Add breakpoint in visual studio 2022 or laters as you need to in stepping. The app window will open with a blank pane and a click me button.
It is basically the Winui 3 mainwindow template from the VS studio winui3 template projects. Click the button and the app startes running its test. You should notice a successful
call to geteventproperties, but throw a SOAP action fault for GetServiceCapabilities if you have an Axis device running 10.12.199. This bug has been reproduced with an Axis Q6135LE as the target test device


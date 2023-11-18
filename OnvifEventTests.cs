﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using OnvifDeviceManagement;

using System.Net;

using Windows.Media.Protection.PlayReady;
using System.Diagnostics;
using Microsoft.UI.Composition;

using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;


using static System.Net.WebRequestMethods;

namespace AxisDemoSample
{
    public class OnvifEventTests
    {

        private string xaddr = "http://192.168.0.13/onvif/device_service";
        
        private string loginName = "UserName";
        private string password = "The Password";
        private Uri deviceServiceUri;


        private DeviceChannel deviceChannel = null;
        private bool channelOpened = false;

        private OnvifDeviceManagement.GetCapabilitiesResponse deviceCapabilitiesResponse;
        private OnvifDeviceManagement.GetServiceCapabilitiesResponse serviceCapabilitiesResponse;
        private OnvifDeviceManagement.GetServicesResponse servicesResponse;

        private bool okToProceed = false;


        public OnvifEventTests()
        {

            // You need to populate the following xaddr login name and paswword with the axis device Onvif service xaddr, and Onvif username and passwrod account.
            // It is assumed that the Onvif user account has adminsitrator privileges.

            /*
            this.xaddr = "XXXXX";
            this.loginName = "SeeOnvif";
            this.password = "ThePassword";
            */
        }

        public async void InitializeAsync()
        {
            bool success = false;

            success = await InitializeDeviceServiceChannelAndXaddressesAsync();

            if(success)
            {
                okToProceed = true;
                bool testDone = await ConductEventTestsAsync();
            }
            else
            {
                okToProceed = false;
            }

        }


        private string media1Xaddr = null;
        private string media2Xaddr = null;
        private string ptzXaddr = null;
        private string eventsXaddr = null;
        private string deviceIOXaddr = null;
        private string imagingXaddr = null;
        private string deviceServiceXaddr = null;

        public async Task<bool> InitializeDeviceServiceChannelAndXaddressesAsync()
        {

            if (Uri.TryCreate(this.xaddr, UriKind.Absolute, out deviceServiceUri))
            {
                // Use http digest authentication.

                HttpTransportBindingElement httpTransport = new HttpTransportBindingElement { AuthenticationScheme = AuthenticationSchemes.Digest };

                TextMessageEncodingBindingElement textEnc = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8);

                CustomBinding custBinding = new CustomBinding(textEnc, httpTransport);

                EndpointAddress endP = new EndpointAddress(deviceServiceUri);

                ChannelFactory<DeviceChannel> factory = new ChannelFactory<DeviceChannel>(custBinding, endP);

                factory.Credentials.UserName.UserName = loginName;
                factory.Credentials.UserName.Password = password;
                factory.Credentials.HttpDigest.ClientCredential.UserName = loginName;
                factory.Credentials.HttpDigest.ClientCredential.Password = password;

                deviceChannel = factory.CreateChannel();

                deviceChannel.Opening += DeviceChannel_Opening;
                deviceChannel.Opened += DeviceChannel_Opened;
                deviceChannel.Closing += DeviceChannel_Closing;
                deviceChannel.Closed += DeviceChannel_Closed;
                deviceChannel.Faulted += DeviceChannel_Faulted;



            }
            else
            {
                throw new ArgumentException("Bad devicechannel service address in device Soap binding Constructor");
            }
            try
            {
                deviceChannel.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception trying to open a channel in onvif device: " + ex.Message);
                okToProceed = false;
                return false;
            }
            
            try
            {

                deviceCapabilitiesResponse = await deviceChannel.GetCapabilitiesAsync(new OnvifDeviceManagement.GetCapabilitiesRequest());
                serviceCapabilitiesResponse = await deviceChannel.GetServiceCapabilitiesAsync(new OnvifDeviceManagement.GetServiceCapabilitiesRequest());
                servicesResponse = await deviceChannel.GetServicesAsync(new OnvifDeviceManagement.GetServicesRequest());
                OnvifDeviceManagement.GetUsersResponse users = await deviceChannel.GetUsersAsync(new OnvifDeviceManagement.GetUsersRequest());


                foreach (OnvifDeviceManagement.Service service in servicesResponse.Service)
                {
                    if (service.Namespace == "http://www.onvif.org/ver10/device/wsdl")
                    {
                        deviceServiceXaddr = service.XAddr;
                    }

                    if (service.Namespace == "http://www.onvif.org/ver20/media/wsdl")
                    {
                        media2Xaddr = service.XAddr;

                    }
                    if (service.Namespace == "http://www.onvif.org/ver10/media/wsdl")
                    {
                        media1Xaddr = service.XAddr;

                    }
                    if (service.Namespace == "http://www.onvif.org/ver20/ptz/wsdl")
                    {
                        ptzXaddr = service.XAddr;
                    }
                    if (service.Namespace == "http://www.onvif.org/ver10/events/wsdl")
                    {
                        eventsXaddr = service.XAddr;
                    }
                    if (service.Namespace == "http://www.onvif.org/ver10/deviceIO/wsdl")
                    {
                        deviceIOXaddr = service.XAddr;
                    }
                    if (service.Namespace == "http://www.onvif.org/ver20/imaging/wsdl")
                    {
                        imagingXaddr = service.XAddr;
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception trying to get initial capabilities etc from onvif profile G device: " + ex.Message);
                okToProceed = false;
                return false;
            }
            finally
            {
                deviceChannel.Close();
            }
            
            
            okToProceed = true;

            return true;
        }


        public async Task<bool> ConductEventTestsAsync()
        {


            // First we construct a WCF stack binding for SOAP12 messages and open the channel to the device.

            OnvifEvents.EventPortTypeClient evClient = null;

            if (this.eventsXaddr == null) throw new ArgumentNullException("Null deviceclient service address in custom binding Constructor");

            Uri eventsServiceUri = null;

            if (Uri.TryCreate(this.xaddr, UriKind.Absolute, out eventsServiceUri))
            {
                // Use http digest authentication.

                HttpTransportBindingElement httpTransport = new HttpTransportBindingElement { AuthenticationScheme = AuthenticationSchemes.Digest };

                TextMessageEncodingBindingElement textEnc = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8);

                CustomBinding custBinding = new CustomBinding(textEnc, httpTransport);

                EndpointAddress endP = new EndpointAddress(eventsServiceUri);

                evClient = new OnvifEvents.EventPortTypeClient(custBinding, endP);

                evClient.ClientCredentials.UserName.UserName = this.loginName;
                evClient.ClientCredentials.UserName.Password = this.password;
                evClient.ClientCredentials.HttpDigest.ClientCredential.UserName = this.loginName;
                evClient.ClientCredentials.HttpDigest.ClientCredential.Password = this.password;

            }
            else
            {
                throw new ArgumentException("Bad devicechannel service address in device Soap binding Constructor");
            }
            
            // Open the channel, get the eventproperties to perove that we can make a successful call, adn then make the getservicecapabilities to demonstrate the fault on the axis device.

            await evClient.OpenAsync();

            OnvifEvents.GetEventPropertiesResponse cvb = await evClient.GetEventPropertiesAsync(new OnvifEvents.GetEventPropertiesRequest());
            OnvifEvents.TopicSetType tst = cvb.TopicSet;

            OnvifEvents.GetServiceCapabilitiesResponse capRes = await evClient.GetServiceCapabilitiesAsync(new OnvifEvents.GetServiceCapabilitiesRequest());

            await evClient.CloseAsync();

            return true;
        }

        private void DeviceChannel_Faulted(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void DeviceChannel_Closed(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void DeviceChannel_Closing(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void DeviceChannel_Opened(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void DeviceChannel_Opening(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
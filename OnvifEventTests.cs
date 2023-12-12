using System;
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
using OnvifEvents;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Schema;
using System.Xml;

namespace AxisDemoSample
{
    public class OnvifEventTests
    {

        private string xaddr;
        
        private string loginName;
        private string password;
        private Uri deviceServiceUri;


        private DeviceChannel deviceChannel = null;
        private bool channelOpened = false;

        private OnvifDeviceManagement.GetCapabilitiesResponse deviceCapabilitiesResponse;
        private OnvifDeviceManagement.GetServiceCapabilitiesResponse serviceCapabilitiesResponse;
        private OnvifDeviceManagement.GetServicesResponse servicesResponse;

        private bool okToProceed = false;

        private Regex subscriptionIDRegex;

        public OnvifEventTests()
        {

            // You need to populate the following xaddr login name and paswword with the axis device Onvif service xaddr, and Onvif username and passwrod account.
            // It is assumed that the Onvif user account has adminsitrator privileges.

            
            this.xaddr = "http://192.168.0.13/onvif/device_service";
            this.loginName = "SeeOnvif";
            this.password = "password";

            subscriptionIDRegex = new Regex(@"^<dom0:SubscriptionId.+>(?'evchannel'\d+)</dom0:SubscriptionId>$", RegexOptions.IgnoreCase);
            
        }

        public OnvifEventTests(string deviceServiceXaddr, string userName, string password)
        {

            this.xaddr = deviceServiceXaddr;
            this.loginName= userName;
            this.password = password;

            subscriptionIDRegex = new Regex(@"^<dom0:SubscriptionId.+>(?'evchannel'\d+)</dom0:SubscriptionId>$", RegexOptions.IgnoreCase);
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
                 throw;
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

            /*
             // Call to GetServiceCapabilitiesAsync commented out as the bug has been confirmed in Axis and no point faulting on it or testing until rectification.
             
            OnvifEvents.GetServiceCapabilitiesResponse capRes = await evClient.GetServiceCapabilitiesAsync(new OnvifEvents.GetServiceCapabilitiesRequest());
            */


            // Now lets create a pullpointsubscription so we can subscribe to events.
            // In this case we will set the subscription time for 5 minutes, and go for all topics which were retrieved in the previous call to GetEventPropertiesAsync.

            OnvifEvents.CreatePullPointSubscriptionResponse pc = await evClient.CreatePullPointSubscriptionAsync(new OnvifEvents.CreatePullPointSubscriptionRequest(null, "PT5M", null, tst.Any));

            // We now have the pullpointsubscription created for us by the Axis device. The critical parameter is the pullpoint subscription reference
            // which gives us the endpoint for creating a subscription client and then start pulling event messages.

            OnvifEvents.ReferenceParametersType refParams = pc.SubscriptionReference.ReferenceParameters;
            OnvifEvents.AttributedURIType subscriptionsUri = pc.SubscriptionReference.Address;


            string subId = refParams.Any[0].OuterXml;

            /*
            Match subMatch = subscriptionIDRegex.Match(subId);
            string sUristring = null;
            if (subMatch.Success)
            {
                sUristring = subscriptionsUri.Value; // + "?" + refParams.Any[0].Name + "=" + refParams.Any[0].FirstChild.Value;
            }
            else
            {
                throw new ArgumentException("bad subscription ID reference parameter returned from Axis DUT");
            }

            */

            Uri pullPointServiceUri = null;

            if (Uri.TryCreate(subscriptionsUri.Value, UriKind.Absolute, out pullPointServiceUri))
            {
                HttpTransportBindingElement httpTransport = new HttpTransportBindingElement { AuthenticationScheme = AuthenticationSchemes.Digest };

                TextMessageEncodingBindingElement textEnc = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8);

                CustomBinding custBinding = new CustomBinding(textEnc, httpTransport);

                EndpointAddress endP = new EndpointAddress(pullPointServiceUri);


                OnvifEvents.PullPointSubscriptionClient subclient = new OnvifEvents.PullPointSubscriptionClient(custBinding, endP);

                subclient.ClientCredentials.UserName.UserName = this.loginName;
                subclient.ClientCredentials.UserName.Password = this.password;
                subclient.ClientCredentials.HttpDigest.ClientCredential.UserName = this.loginName;
                subclient.ClientCredentials.HttpDigest.ClientCredential.Password = this.password;

                subclient.ChannelFactory.Endpoint.EndpointBehaviors.Add(new SimpleEndpointBehavior());

                await subclient.OpenAsync();

                // This call unsurprisingly produces a response from the Axis server of "400 Bad Request" since it is pretty clear the subscription address is malformed in some way
                // and does not include the subscription ID as a parameter in the Uri.
                OnvifEvents.PullMessagesResponse messages = await subclient.PullMessagesAsync(new PullMessagesRequest("PT5M", 2, refParams.Any));
                
                await subclient.CloseAsync();

                await evClient.CloseAsync();
            }

            return true;
        }

        // Client message inspector  
        public class SimpleMessageInspector : IClientMessageInspector
        {
            public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
            {
                // Implement this method to inspect/modify messages after a message  
                // is received but prior to passing it back to the client
                Debug.WriteLine("AfterReceiveReply called");

                string r = reply.ToString();
            }

            public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
            {
                // Implement this method to inspect/modify messages before they
                // are sent to the service  
                Debug.WriteLine("BeforeSendRequest called");
                string msString = request.ToString();
                System.ServiceModel.Channels.Message message = request as System.ServiceModel.Channels.Message;

                // pull body into a memory backed writer
                XmlDictionaryReaderQuotas quotas =
                new XmlDictionaryReaderQuotas();
                XmlReader bodyReader =
                    message.GetReaderAtBodyContents().ReadSubtree();
                XmlReaderSettings wrapperSettings =
                                      new XmlReaderSettings();
                wrapperSettings.CloseInput = true;
                //wrapperSettings.Schemas = schemaSet;
                wrapperSettings.ValidationFlags =
                                        XmlSchemaValidationFlags.None;
                wrapperSettings.ValidationType = ValidationType.Schema;
                //wrapperSettings.ValidationEventHandler += new
                //ValidationEventHandler(InspectionValidationHandler);
                XmlReader wrappedReader = XmlReader.Create(bodyReader,
                                                    wrapperSettings);
                //string ms = wrappedReader.Read;

                // Now write it out again.

                MemoryStream memStream = new MemoryStream();
                XmlDictionaryWriter xdw = XmlDictionaryWriter.CreateBinaryWriter(memStream);
                xdw.WriteNode(wrappedReader, false);
                xdw.Flush(); memStream.Position = 0;
                XmlDictionaryReader xdr = XmlDictionaryReader.CreateBinaryReader(memStream, quotas);
                
                // reconstruct the message with any modifications made

                //string ty = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n\r\n-<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">\r\n\r\n\r\n-<s:Header>\r\n\r\n<a:Action s:mustUnderstand=\"1\">http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesRequest</a:Action>\r\n\r\n<a:MessageID>urn:uuid:2e5eb691-d5b8-4e74-b9f5-d375db21a87d</a:MessageID>\r\n\r\n\r\n-<a:ReplyTo>\r\n\r\n<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>\r\n\r\n</a:ReplyTo>\r\n\r\n</s:Header>\r\n\r\n\r\n-<s:Body xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n\r\n\r\n-<PullMessages xmlns=\"http://www.onvif.org/ver10/events/wsdl\">\r\n\r\n<Timeout>PT5M</Timeout>\r\n\r\n<MessageLimit>1</MessageLimit>\r\n\r\n<dom0:SubscriptionId xmlns:dom0=\"http://www.axis.com/2009/event\">1</dom0:SubscriptionId>\r\n\r\n</PullMessages>\r\n\r\n</s:Body>\r\n\r\n</s:Envelope>";

                Message replacedMessage =
                    Message.CreateMessage(message.Version, null, xdr);
                replacedMessage.Headers.CopyHeadersFrom(message.Headers);
                replacedMessage.Properties.CopyProperties(message.Properties);
                request = replacedMessage;

                return null;
            }
        }

        // Endpoint behavior  
        public class SimpleEndpointBehavior : IEndpointBehavior
        {
            public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
            {
                // No implementation necessary  
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
                clientRuntime.ClientMessageInspectors.Add(new SimpleMessageInspector());
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                // No implementation necessary  
            }

            public void Validate(ServiceEndpoint endpoint)
            {
                // No implementation necessary  
            }
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

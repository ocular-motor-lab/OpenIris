//-----------------------------------------------------------------------
// <copyright file="EyeTrackerService.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Diagnostics;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.Text;

    /// <summary>
    /// Service methods to allow remote control of the eye tracker.
    /// </summary>
    public class EyeTrackerRemoteServices
    {
        private static ServiceHost? eyeTrackerHostNetTcp;
        private static ServiceHost? eyeTrackerHostWeb;
        private static EyeTrackerTcpListener? eyeTrackerHostTcp = null;
        private static EyeTrackerUdpListener? eyeTrackerHostUdp = null;

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="newEyeTracker">Eye tracker object.</param>
        /// <returns>The service host object.</returns>
        public static void Start(EyeTracker newEyeTracker)
        {
            var eyeTracker = new EyeTrackerRemote(newEyeTracker);

            // NET TCP SERVICE
            try
            {
                eyeTrackerHostNetTcp = new ServiceHost(eyeTracker);
                var binding = new NetTcpBinding();
                binding.MaxReceivedMessageSize = 2147483647;
                binding.Security.Mode = SecurityMode.None;

                var address = "net.tcp://localhost:" + newEyeTracker.Settings.ServiceListeningPort + "/EyeTrackerEndpoint";
                var e = eyeTrackerHostNetTcp.AddServiceEndpoint(typeof(IEyeTrackerService), binding, address);

                eyeTrackerHostNetTcp.Open();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting NET TCP service: " + ex.Message);
                eyeTrackerHostNetTcp = null;
            }

            // HTTP SERVICE
            try
            {
                eyeTrackerHostWeb = new ServiceHost(eyeTracker, new Uri[] { new Uri("http://localhost:" + (newEyeTracker.Settings.ServiceListeningPort + 1) + "/EyeTrackerEndpoint") });
                eyeTrackerHostWeb.AddServiceEndpoint(typeof(IEyeTrackerWebService), new BasicHttpBinding(), "Soap");
                var endpoint = eyeTrackerHostWeb.AddServiceEndpoint(typeof(IEyeTrackerWebService), new WebHttpBinding(WebHttpSecurityMode.None), "Web");
                endpoint.EndpointBehaviors.Add(new WebHttpBehavior());
                eyeTrackerHostWeb.Open();
                Trace.WriteLine(eyeTrackerHostWeb.State);
                Trace.WriteLine(eyeTrackerHostWeb.Description);
                Trace.WriteLine(eyeTrackerHostWeb.Credentials);
                if (eyeTrackerHostWeb.BaseAddresses.Count > 0)
                    Trace.WriteLine(eyeTrackerHostWeb.BaseAddresses[0].ToString());

                Trace.WriteLine(endpoint.Address);
                Trace.WriteLine(endpoint.ListenUri);
                Trace.WriteLine(endpoint.ListenUriMode);
                Trace.WriteLine(endpoint.EndpointBehaviors[0]);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting WEB service: " + ex.Message);


                Trace.WriteLine("==== INSTRUCTIONS ==========================================================");
                Trace.WriteLine("After running the server for the first time you should open the command line");
                Trace.WriteLine("in administrator mode and run the following command to enable the service.");
                Trace.WriteLine($"netsh http add urlacl url = http://+:{newEyeTracker.Settings.ServiceListeningPort + 1}/EyeTrackerEndpoint/ user=username");
                Trace.WriteLine("After running the command and starting the eye tracker you can open the browser ");
                Trace.WriteLine($"http://localhost:{newEyeTracker.Settings.ServiceListeningPort + 1}/EyeTrackerEndpoint/Web/StartRecording");
                Trace.WriteLine($"Or from a different computer using the correct IP address");
                Trace.WriteLine("==== END INSTRUCTIONS ==========================================================");


                eyeTrackerHostWeb = null;
            }

            // TCP SERVICE socket
            try
            {
                eyeTrackerHostTcp = new EyeTrackerTcpListener(newEyeTracker, newEyeTracker.Settings.ServiceListeningPort + 2);
                eyeTrackerHostTcp.Start();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting TCP service: " + ex.Message);
                eyeTrackerHostTcp = null;
            }

            // UDP SERVICE socket
            try
            {
                eyeTrackerHostUdp = new EyeTrackerUdpListener(newEyeTracker, newEyeTracker.Settings.ServiceListeningPort + 3);
                eyeTrackerHostUdp.Start();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting UCP service: " + ex.Message);
                eyeTrackerHostUdp = null;
            }


            // HTTP SERVICE FROM CLEVELAND DO NOT CHANGE
            //Need to run something like this in the command line. 
            //netsh http add urlacl url = http://+:9001/EyeTrackerEndpoint/ user=jorge
            // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-create-a-basic-wcf-web-http-service
            //
            //var host = new ServiceHost(typeof(EyeTrackerRemoteService), new Uri[] { new Uri("http://10.77.17.88:" + EyeTrackerRemoteService.EyeTracker.Settings.ServiceListeningPort + "/EyeTrackerEndpoint") });
            //host.AddServiceEndpoint(typeof(IEyeTrackerService), new BasicHttpBinding(), "Soap");
            //var endpoint = host.AddServiceEndpoint(typeof(IEyeTrackerService), new WebHttpBinding(), "Web");
            //endpoint.EndpointBehaviors.Add(new WebHttpBehavior());
            //host.Open();

            // END HTTP SERVICE FROM CLEVELAND DO NOT CHANGE
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public static void StopService()
        {
            try
            {
                eyeTrackerHostNetTcp?.Close();
                eyeTrackerHostWeb?.Close();
                eyeTrackerHostTcp?.Stop();
                eyeTrackerHostUdp?.Stop();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error closing the service: " + ex.Message);
            }
        }

        /// <summary>
        /// Gets the IP addresses
        /// </summary>
        /// <returns></returns>
        public static string GetIPAddresses()
        {
            StringBuilder sb = new StringBuilder();

            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection) 
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
                // Read the IP configuration for each network 
                IPInterfaceProperties properties = network.GetIPProperties();

                // Each network interface may have multiple IP addresses 
                foreach (IPAddressInformation address in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now 
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1) 
                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    sb.AppendLine(address.Address.ToString() + " (" + network.Name + ")");
                }
            }

            return sb.ToString();
        }
    }
}

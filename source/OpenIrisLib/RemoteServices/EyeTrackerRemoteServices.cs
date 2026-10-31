//-----------------------------------------------------------------------
// <copyright file="EyeTrackerService.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
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
    using System.Text;

    /// <summary>
    /// Service methods to allow remote control of the eye tracker.
    /// </summary>
    public class EyeTrackerRemoteServices
    {
        private static EyeTrackerWebServer? eyeTrackerWebServer;
        private static EyeTrackerTcpListener? eyeTrackerHostTcp = null;
        private static EyeTrackerUdpListener? eyeTrackerHostUdp = null;
        private static EyeTrackerLslOutlet? eyeTrackerLslOutlet = null;

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="newEyeTracker">Eye tracker object.</param>
        /// <returns>The service host object.</returns>
        public static void Start(EyeTracker newEyeTracker)
        {
            // WEB SERVER: REST API, live data and web client. It replaces the WCF web endpoint
            // that was on this port, and keeps its URLs (/EyeTrackerEndpoint/Web/StartRecording...).
            try
            {
                eyeTrackerWebServer = new EyeTrackerWebServer(newEyeTracker, newEyeTracker.Settings.ServiceListeningPort + 1);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting the web server: " + ex.Message);
                eyeTrackerWebServer = null;
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

            // LSL OUTLET (data sink running in parallel with any eye tracking system)
            try
            {
                eyeTrackerLslOutlet = new EyeTrackerLslOutlet(newEyeTracker);
                eyeTrackerLslOutlet.Start();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error starting LSL outlet: " + ex.Message);
                eyeTrackerLslOutlet = null;
            }
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public static void StopService()
        {
            try
            {
                eyeTrackerWebServer?.Dispose();
                eyeTrackerHostTcp?.Stop();
                eyeTrackerHostUdp?.Stop();
                eyeTrackerLslOutlet?.Stop();
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

////=============================================================================
//// Copyright (c) 2001-2019 FLIR Systems, Inc. All Rights Reserved.
////
//// This software is the confidential and proprietary information of FLIR
//// Integrated Imaging Solutions, Inc. ("Confidential Information"). You
//// shall not disclose such Confidential Information and shall use it only in
//// accordance with the terms of the license agreement you entered into
//// with FLIR Integrated Imaging Solutions, Inc. (FLIR).
////
//// FLIR MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY OF THE
//// SOFTWARE, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE
//// IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//// PURPOSE, OR NON-INFRINGEMENT. FLIR SHALL NOT BE LIABLE FOR ANY DAMAGES
//// SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//// THIS SOFTWARE OR ITS DERIVATIVES.
////=============================================================================

///**
// *	@example Enumeration_CSharp.cs
// *
// *  @brief Enumeration_CSharp.cs shows how to enumerate interfaces and cameras. 
// *	Knowing this is mandatory for doing anything with the Spinnaker SDK, and is 
// *	therefore the best place to start learning how to use the SDK.
// *
// *	This example introduces the preparation, use, and cleanup of the system
// *	object, interface and camera lists, interfaces, and cameras. It also 
// *  touches on retrieving both nodes from nodemaps and information from nodes.
// *
// *	Once comfortable with enumeration, we suggest checking out 
// *  Acquisition_CSharp, NodeMapInfo_CSharp, or ExceptionHandling_CSharp 
// *  examples. Acquisition_CSharp demonstrates using a camera to acquire images,
// *  ExceptionHandling_CSharp explores the use of standard and Spinnaker 
// *  exceptions, and NodeMapInfo_CSharp demonstrates retrieving information 
// *  from various node types.
// */

//using System;
//using System.Collections.Generic;
//using SpinnakerNET;
//using SpinnakerNET.GenApi;

//namespace Enumeration_CSharp
//{
//    class Program
//    {
//        // This function queries an interface for its cameras and then prints 
//        // out device information.
//        int QueryInterface(IManagedInterface managedInterface)
//        {
//            int result = 0;

//            try
//            {
//                //
//                // Retrieve TL nodemap from interface
//                //
//                // *** NOTES ***
//                // Each interface has a nodemap that can be retrieved in order 
//                // to access information about the interface itself, any devices 
//                // connected, or addressing information if applicable.
//                //
//                INodeMap nodeMapInterface = managedInterface.GetTLNodeMap();

//                // 
//                // Print interface display name
//                //
//                // *** NOTES ***
//                // Grabbing node information requires first retrieving the node 
//                // and then retrieving its information. There are two things to
//                // keep in mind. First, a node is distinguished by type, which
//                // is related to its value's data type. Second, nodes should be 
//                // checked for availability and readability/writability prior to
//                // making an attempt to read from or write to them.
//                //
//                IString iInterfaceDisplayName = nodeMapInterface.GetNode<IString>("InterfaceDisplayName");

//                if (iInterfaceDisplayName != null && iInterfaceDisplayName.IsReadable)
//                {
//                    string interfaceDisplayName = iInterfaceDisplayName.Value;

//                    Console.WriteLine("{0}", interfaceDisplayName);
//                }
//                else
//                {
//                    Console.WriteLine("Interface display name not readable");
//                }

//                //
//                // Update list of cameras on the interface
//                //
//                // *** NOTES ***
//                // Updating the cameras on each interface is especially important 
//                // if there has been any device arrivals or removals since the 
//                // last time UpdateCameras() was called.
//                //
//                managedInterface.UpdateCameras();

//                //
//                // Retrieve list of cameras from the interface
//                //
//                // *** NOTES ***
//                // Camera lists can be retrieved from an interface or the system 
//                // object. Camera lists retrieved from an interface, such as this
//                // one, only return cameras attached on that specific interface 
//                // while camera lists retrieved from system returns all cameras 
//                // on all interfaces.
//                //
//                // *** LATER ***
//                // Camera lists must be cleared manually. This must be done 
//                // prior to releasing the system and while the camera list is 
//                // still in scope.
//                // 
//                ManagedCameraList camList = managedInterface.GetCameras();

//                // Return if no cameras detected
//                if (camList.Count == 0)
//                {
//                    Console.WriteLine("\tNo devices detected.\n");
//                    return 0;
//                }

//                // Print device vendor and model name for each camera on the 
//                // interface
//                for (int i = 0; i < camList.Count; i++)
//                {
//                    //
//                    // Select camera
//                    //
//                    // *** NOTES ***
//                    // Each camera is retrieved from a camera list with an index. 
//                    // If the index is out of range, an exception is thrown.
//                    // 
//                    IManagedCamera cam = camList[i];

//                    // Retrieve TL device nodemap; please see NodeMapInfo_CSharp
//                    // example for additional information on TL device nodemaps
//                    INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

//                    Console.Write("\tDevice {0} ", i);

//                    // Print device vendor name and device model name
//                    IString iDeviceVendorName = nodeMapTLDevice.GetNode<IString>("DeviceVendorName");

//                    if (iDeviceVendorName != null && iDeviceVendorName.IsReadable)
//                    {
//                        String deviceVendorName = iDeviceVendorName.Value;

//                        Console.Write("{0} ", deviceVendorName);
//                    }

//                    IString iDeviceModelName = nodeMapTLDevice.GetNode<IString>("DeviceModelName");

//                    if (iDeviceModelName != null && iDeviceModelName.IsReadable)
//                    {
//                        String deviceModelName = iDeviceModelName.Value;

//                        Console.WriteLine("{0}\n", deviceModelName);
//                    }

//                    // Dispose of managed camera
//                    cam.Dispose();

//                    //
//                    // Clear camera list before losing scope
//                    //
//                    // *** NOTES ***
//                    // If a camera list (or an interface list) is not cleaned up
//                    // manually, the system will do so when the system is 
//                    // released.
//                    //
//                    camList.Clear();
//                }
//            }
//            catch (SpinnakerException ex)
//            {
//                Console.WriteLine("Error " + ex.Message);
//                result = -1;
//            }

//            return result;
//        }

//        // Example entry points; this function sets up the system and retrieves 
//        // interfaces to retrieves interfaces for the example.
//        static int Main(string[] args)
//        {
//            int result = 0;

//            Program program = new Program();

//            // 
//            // Retrieve singleton reference to system object
//            //
//            // *** NOTES ***
//            // Everything originates with the system object. It is important to 
//            // notice that it has a singleton implementation, so it is impossible 
//            // to have multiple system objects at the same time.
//            // 
//            // *** LATER ***
//            // The system object should be cleared prior to program completion.
//            // If not released explicitly, it will be released automatically 
//            // when all system objects go out of scope.
//            //
//            ManagedSystem system = new ManagedSystem();

//            // Print out current library version
//            LibraryVersion spinVersion = system.GetLibraryVersion();
//            Console.WriteLine("Spinnaker library version: {0}.{1}.{2}.{3}\n\n",
//                              spinVersion.major,
//                              spinVersion.minor,
//                              spinVersion.type,
//                              spinVersion.build);

//            //
//            // Retrieve list of interfaces from the system
//            //
//            // *** NOTES ***
//            // Interface lists are retrieved from the system object.
//            // 
//            // *** LATER ***
//            // ManagedInterfaceList inherits from native C# List and is constructed 
//            // using list objects of IManagedInterface objects. ManagedInterfaceList must be 
//            // cleared after use.
//            // 
//            ManagedInterfaceList interfaceList = system.GetInterfaces();

//            Console.WriteLine("Number of interfaces detected: {0}\n", interfaceList.Count);

//            //
//            // Retrieve list of cameras from the system
//            //
//            // *** NOTES ***
//            // Camera lists are retrieved from the system object.
//            // 
//            // *** LATER ***
//            // Camera lists are constructed using list objects of IManagedCamera
//            // objects. Lists are native to C# and must be cleared after use.
//            // 
//            ManagedCameraList camList = system.GetCameras();

//            Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);

//            // Finish if there are no cameras
//            if (camList.Count == 0 || interfaceList.Count == 0)
//            {
//                // Clear camera list before releasing system
//                camList.Clear();

//                // Clear interface list before releasing system
//                interfaceList.Clear();

//                // Release system
//                system.Dispose();

//                Console.WriteLine("Not enough cameras!");
//                Console.WriteLine("Done! Press Enter to exit...");
//                Console.ReadLine();

//                return -1;
//            }

//            Console.WriteLine("\n*** QUERYING INTERFACES ***\n");

//            //
//            // Run example on each interface
//            //
//            // *** NOTES ***
//            // Managed interfaces will need to be disposed of after use in order
//            // to fully clean up. Using-statements help ensure that this is taken
//            // care of; otherwise, interfaces can be disposed of manually by calling
//            // Dispose().
//            //
//            foreach (IManagedInterface managedInterface in interfaceList)
//                using (managedInterface)
//                {
//                    try
//                    {
//                        // Run example
//                        result = result | program.QueryInterface(managedInterface);
//                    }
//                    catch (SpinnakerException ex)
//                    {
//                        Console.WriteLine("Error: {0}", ex.Message);
//                        result = -1;
//                    }
//                }

//            //
//            // Clear camera list before releasing system
//            //
//            // *** NOTES ***
//            // If a camera list is not cleaned up
//            // manually, the system will do so when System.Dispose() is
//            // called.
//            //
//            camList.Clear();

//            //
//            // Clear interface list before releasing system
//            //
//            // *** NOTES ***
//            // If an interface list is not cleaned up
//            // manually, the system will do so when System.Dispose() is
//            // called.
//            //
//            interfaceList.Clear();

//            //
//            // Release system
//            //
//            // *** NOTES ***
//            // The system should be released, but if it is not, it will do so
//            // itself. It is often at the release of the system (whether manual 
//            // or automatic) that unbroken references and still registered 
//            // events will throw an exception.
//            //
//            system.Dispose();

//            Console.WriteLine("\nDone! Press Enter to exit...");
//            Console.ReadLine();

//            return result;
//        }
//    }
//}

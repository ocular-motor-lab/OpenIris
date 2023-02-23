//-----------------------------------------------------------------------
// <copyright file="DataAcquisition.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
    using System;
    using System.Threading;
    using MccDaq;

    /// <summary>
    /// Data from the data acquisition board.
    /// </summary>
    [Serializable]
    public struct DaqData
    {
        /// <summary>
        /// Counter (useful for detecting short triggers).
        /// </summary>
        public int Counter {get; set;}

        /// <summary>
        /// Analog channel 0.
        /// </summary>
        public int Analog0 { get; set; }

        /// <summary>
        /// Analog channel 1.
        /// </summary>
        public int Analog1 { get; set; }

        /// <summary>
        /// Analog channel 2.
        /// </summary>
        public int Analog2 { get; set; }

        /// <summary>
        /// Analog channel 3.
        /// </summary>
        public int Analog3 { get; set; }
    }

    /// <summary>
    /// Class to control a USB acquisition board.
    /// </summary>
    internal class DataAcquisitionMeasurementComputing
    {
        private static DataAcquisitionMeasurementComputing singleton;

        public static DataAcquisitionMeasurementComputing Instance
        {
            get
            {
                if (singleton is null)
                {
                    singleton = new DataAcquisitionMeasurementComputing();

                    try
                    {
                        singleton.daqBoard = new MccDaq.MccBoard(0);

                        singleton.daqBoard.DConfigPort(DigitalPortType.FirstPortA, DigitalPortDirection.DigitalIn);
                        singleton.daqBoard.DConfigPort(DigitalPortType.FirstPortB, DigitalPortDirection.DigitalOut);
                    }
                    catch(Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("Error initializing the DataAcquisitionMeasurementComputing. "+ex.Message);

                        singleton.daqBoard = null;
                    }
                }
                return singleton;
            }
        }

        /// <summary>
        /// Board controller.
        /// </summary>
        private MccBoard daqBoard;

        /// <summary>
        /// Reads the counter from the board.
        /// </summary>
        /// <remarks>The counter counts all the triggers in one of the inputs.</remarks>
        /// <returns>The counter value.</returns>
        internal int ReadCounter()
        {
            uint c = 0;

            if (this.daqBoard != null)
            {
                this.daqBoard.CIn32(1, out c);
            }

            return (int)c;
        }

        /// <summary>
        /// Reads a analog port.
        /// </summary>
        /// <param name="portNumber">Port number.</param>
        /// <returns>Value read from the port.</returns>
        internal short ReadAnalog(int portNumber)
        {
            short dataValue = 0;
            
            if (this.daqBoard != null)
            {
                this.daqBoard.AIn(portNumber, MccDaq.Range.Bip10Volts, out dataValue);
            }

            return dataValue;
        }
    }
}

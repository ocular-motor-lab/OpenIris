//-----------------------------------------------------------------------
// <copyright file="SoundRecorder.cs" company="Jonhs Hopkins University">
//     Copyright (c) 2014-2020 Jorge Otero-Millan, Oculomotor lab, Johns Hopkins University. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenIris
{
#nullable disable
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1031 // Do not catch general exception types

    using System;

    /// <summary>
    /// Class to record sounds from the microphone.
    /// </summary>
    /// <remarks>Inspired in Author : Mohamed Shimran Blog : http://www.ultimateprogrammingtutorials.blogspot.com </remarks>
    [Obsolete]
    public class SoundRecorder
    {
        private readonly string fileName;

        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int mciSendString(string MciCommand, string MciReturn, int MciReturnLength, int CallBack);
        /// <summary>
        /// Initializes a new instance of the SoundRecorder class.
        /// </summary>
        public SoundRecorder(string fileName)
        {
            this.fileName = fileName;
        }

                              /// <summary>
                              /// Start to record audio from the microphone.
                              /// </summary>
        public void StartRecording()
        {
            try
            {
                OpenWave();
                Record();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Pause the recording and save the file to disk.
        /// </summary>
        public void StopRecording()
        {
            try
            {
                Pause();
                Save(this.fileName);
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Opens a wave file.
        /// </summary>
        private static void OpenWave()
        {
            try
            {
                var result = mciSendString("open new type waveaudio alias Som", null, 0, 0);
                if (result != 0)
                {
                    throw new Exception("Error in mciSendString: " + result.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Starts recording audio.
        /// </summary>
        private static void Record()
        {
            try
            {
                var result = mciSendString("record Som", null, 0, 0);
                if (result != 0)
                {
                    throw new Exception("Error in mciSendString: " + result.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Pauses the recording.
        /// </summary>
        private static void Pause()
        {
            try
            {
                var result = mciSendString("pause Som", null, 0, 0);
                if (result != 0)
                {
                    throw new Exception("Error in mciSendString: " + result.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Saves the wave file.
        /// </summary>
        /// <param name="fileName">File name for the wave file.</param>
        private static void Save(string fileName)
        {
            try
            {
                var result = mciSendString("save Som " + fileName, null, 0, 0);
                if (result != 0)
                {
                    throw new Exception("Error in mciSendString: " + result.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Closes the wave file.
        /// </summary>
        private static void Close()
        {
            try
            {
                var result = mciSendString("close Som", null, 0, 0);
                if (result != 0)
                {
                    throw new Exception("Error in mciSendString: " + result.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("SOUND RECORDER ERROR: " + ex.Message);
            }
        }
    }
}
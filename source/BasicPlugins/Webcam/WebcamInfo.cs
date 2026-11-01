//-----------------------------------------------------------------------
// <copyright file="WebcamInfo.cs">
//     Copyright (c) 2026 Jorge Otero-Millan, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using Emgu.CV;
    using Emgu.CV.CvEnum;

    /// <summary>
    /// Writes to the log what the webcams can do: the webcams found, the formats, resolutions and
    /// frame rates of the one used, and what it was opened with. OpenCV can't list formats, so they
    /// come from DirectShow, which numbers the webcams in the same order as OpenCV.
    /// </summary>
    internal static class WebcamInfo
    {
        /// <summary>
        /// Logs the webcams found and the formats of the one used. Call it before opening the webcam.
        /// </summary>
        public static void LogWebcams(int cameraIndex)
        {
            try
            {
                var webcams = GetWebcams();
                if (webcams.Count == 0)
                {
                    Trace.WriteLine("Webcam: no webcams found.");
                    return;
                }

                Trace.WriteLine("Webcams found: " + string.Join(", ", webcams.Select((w, i) => $"{i}: {w.name}")));
                if (cameraIndex < 0 || cameraIndex >= webcams.Count) return;

                var (name, moniker) = webcams[cameraIndex];
                Trace.WriteLine($"Webcam {cameraIndex} ({name}) can send:");
                foreach (var format in GetFormats(moniker)) Trace.WriteLine("    " + format);
                foreach (var w in webcams) Marshal.ReleaseComObject(w.moniker);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Webcam: could not list the webcams and their formats. " + ex.Message);
            }
        }

        /// <summary>
        /// Logs what the webcam was opened with and its settings, as OpenCV sees them.
        /// </summary>
        public static void LogOpened(VideoCapture capture)
        {
            try
            {
                Trace.WriteLine(
                    $"Webcam opened: {capture.Get(CapProp.FrameWidth)}x{capture.Get(CapProp.FrameHeight)}" +
                    $" at {capture.Get(CapProp.Fps):0.##} fps (as reported by the webcam), format {FourCCToString((int)capture.Get(CapProp.FourCC))}");

                var controls = new[]
                {
                    CapProp.Brightness, CapProp.Contrast, CapProp.Saturation, CapProp.Hue, CapProp.Gamma,
                    CapProp.Sharpness, CapProp.Gain, CapProp.Exposure, CapProp.AutoExposure,
                    CapProp.WhiteBalanceBlueU, CapProp.Backlight, CapProp.Focus, CapProp.Autofocus,
                    CapProp.Zoom, CapProp.Pan, CapProp.Tilt,
                };

                // DirectShow gives -1 for the controls a webcam doesn't have.
                var values = controls
                    .Select(control => (control, value: capture.Get(control)))
                    .Where(x => x.value != -1)
                    .Select(x => $"{x.control} {x.value:0.##}");
                Trace.WriteLine("Webcam settings: " + string.Join(", ", values));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Webcam: could not read its settings. " + ex.Message);
            }
        }

        private static List<(string name, IMoniker moniker)> GetWebcams()
        {
            var webcams = new List<(string, IMoniker)>();

            var deviceEnumerator = (ICreateDevEnum)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_SystemDeviceEnum, true));
            try
            {
                var category = CLSID_VideoInputDeviceCategory;
                if (deviceEnumerator.CreateClassEnumerator(ref category, out var monikers, 0) != 0 || monikers is null) return webcams;

                try
                {
                    var moniker = new IMoniker[1];
                    while (monikers.Next(1, moniker, IntPtr.Zero) == 0)
                    {
                        webcams.Add((GetName(moniker[0]), moniker[0]));
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(monikers);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(deviceEnumerator);
            }

            return webcams;
        }

        private static string GetName(IMoniker moniker)
        {
            var bagId = typeof(IPropertyBag).GUID;
            moniker.BindToStorage(null!, null!, ref bagId, out var bagObject);
            var bag = (IPropertyBag)bagObject;
            try
            {
                return bag.Read("FriendlyName", out var name, IntPtr.Zero) == 0 ? (string)name : "unknown";
            }
            finally
            {
                Marshal.ReleaseComObject(bag);
            }
        }

        /// <summary>
        /// Lists the formats of the capture pin, one line per format and resolution with its frame
        /// rates.
        /// </summary>
        private static IEnumerable<string> GetFormats(IMoniker moniker)
        {
            var filterId = typeof(IBaseFilter).GUID;
            moniker.BindToObject(null!, null!, ref filterId, out var filterObject);
            var filter = (IBaseFilter)filterObject;
            var pin = default(IPin);
            try
            {
                pin = FindCapturePin(filter);
                if (pin is null) return new[] { "(no capture pin)" };

                var streamConfig = (IAMStreamConfig)pin;
                var videoControl = filter as IAMVideoControl;

                Marshal.ThrowExceptionForHR(streamConfig.GetNumberOfCapabilities(out var count, out var capsSize));
                var formats = new List<(string format, Size size, string frameRates)>();
                var caps = Marshal.AllocCoTaskMem(Math.Max(capsSize, 128));
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (streamConfig.GetStreamCaps(i, out var mediaTypePointer, caps) != 0) continue;
                        try
                        {
                            var mediaType = Marshal.PtrToStructure<AMMediaType>(mediaTypePointer);
                            if (!TryGetVideoInfo(mediaType, out var size, out var averageInterval)) continue;

                            // Webcams list their frame rates through IAMVideoControl, or give a range.
                            var rates = GetFrameRateList(videoControl, pin, i, size);
                            string frameRates;
                            if (rates.Count > 0)
                            {
                                frameRates = string.Join(", ", rates.Select(r => r.ToString("0.##"))) + " fps";
                            }
                            else if (capsSize >= 120 && Marshal.ReadInt64(caps, 104) > 0)
                            {
                                var max = 1e7 / Marshal.ReadInt64(caps, 104);
                                var min = 1e7 / Marshal.ReadInt64(caps, 112);
                                frameRates = Math.Abs(max - min) < 0.01 ? $"{max:0.##} fps" : $"{min:0.##} to {max:0.##} fps";
                            }
                            else
                            {
                                frameRates = averageInterval > 0 ? $"{1e7 / averageInterval:0.##} fps" : "frame rate unknown";
                            }

                            formats.Add((SubtypeToString(mediaType.SubType), size, frameRates));
                        }
                        finally
                        {
                            FreeMediaType(mediaTypePointer);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(caps);
                }

                return formats
                    .Distinct()
                    .OrderBy(f => f.format)
                    .ThenByDescending(f => f.size.Width * f.size.Height)
                    .Select(f => $"{f.format} {f.size.Width}x{f.size.Height}: {f.frameRates}");
            }
            finally
            {
                if (pin != null) Marshal.ReleaseComObject(pin);
                Marshal.ReleaseComObject(filter);
            }
        }

        /// <summary>
        /// Finds the capture pin, or else the first output pin with formats.
        /// </summary>
        private static IPin? FindCapturePin(IBaseFilter filter)
        {
            Marshal.ThrowExceptionForHR(filter.EnumPins(out var pins));
            if (pins is null) return null;

            IPin? found = null;
            try
            {
                var pin = new IPin[1];
                while (pins.Next(1, pin, IntPtr.Zero) == 0)
                {
                    var keep = false;
                    if (pin[0].QueryDirection(out var direction) == 0 && direction == PINDIR_OUTPUT && pin[0] is IAMStreamConfig)
                    {
                        var isCapture = IsCapturePin(pin[0]);
                        if (found is null || isCapture)
                        {
                            if (found != null) Marshal.ReleaseComObject(found);
                            found = pin[0];
                            keep = true;
                            if (isCapture) break;
                        }
                    }

                    if (!keep) Marshal.ReleaseComObject(pin[0]);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(pins);
            }

            return found;
        }

        private static bool IsCapturePin(IPin pin)
        {
            if (!(pin is IKsPropertySet properties)) return false;

            var propertySet = AMPROPSETID_Pin;
            return properties.Get(ref propertySet, AMPROPERTY_PIN_CATEGORY, IntPtr.Zero, 0, out var category, Marshal.SizeOf<Guid>(), out _) == 0
                && category == PIN_CATEGORY_CAPTURE;
        }

        private static List<double> GetFrameRateList(IAMVideoControl? videoControl, IPin pin, int index, Size size)
        {
            var rates = new List<double>();
            if (videoControl is null) return rates;

            try
            {
                if (videoControl.GetFrameRateList(pin, index, size, out var count, out var list) != 0 || list == IntPtr.Zero) return rates;

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var interval = Marshal.ReadInt64(list, i * sizeof(long));
                        if (interval > 0) rates.Add(1e7 / interval);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(list);
                }
            }
            catch (COMException)
            {
            }

            return rates.Distinct().OrderByDescending(r => r).ToList();
        }

        /// <summary>
        /// Reads the size and frame interval (in 100 ns) of a VIDEOINFOHEADER or VIDEOINFOHEADER2.
        /// </summary>
        private static bool TryGetVideoInfo(AMMediaType mediaType, out Size size, out long averageInterval)
        {
            size = default;
            averageInterval = 0;
            if (mediaType.Format == IntPtr.Zero) return false;

            int bitmapHeader;
            if (mediaType.FormatType == FORMAT_VideoInfo && mediaType.FormatSize >= 88) bitmapHeader = 48;
            else if (mediaType.FormatType == FORMAT_VideoInfo2 && mediaType.FormatSize >= 112) bitmapHeader = 72;
            else return false;

            averageInterval = Marshal.ReadInt64(mediaType.Format, 40);
            size = new Size(
                Marshal.ReadInt32(mediaType.Format, bitmapHeader + 4),
                Math.Abs(Marshal.ReadInt32(mediaType.Format, bitmapHeader + 8)));
            return true;
        }

        private static string SubtypeToString(Guid subtype)
        {
            // Most subtypes are a FOURCC in the first 4 bytes of a fixed GUID.
            var bytes = subtype.ToByteArray();
            if (bytes.Skip(4).SequenceEqual(FourCCGuidTail)) return FourCCToString(BitConverter.ToInt32(bytes, 0));

            return subtype.ToString().ToUpperInvariant() switch
            {
                "E436EB7D-524F-11CE-9F53-0020AF0BA770" => "RGB24",
                "E436EB7E-524F-11CE-9F53-0020AF0BA770" => "RGB32",
                "E436EB7C-524F-11CE-9F53-0020AF0BA770" => "RGB555",
                "E436EB7B-524F-11CE-9F53-0020AF0BA770" => "RGB565",
                _ => subtype.ToString(),
            };
        }

        private static string FourCCToString(int fourCC)
        {
            if (fourCC <= 0) return "unknown";
            var chars = BitConverter.GetBytes(fourCC).Select(b => (char)b).ToArray();
            return chars.All(c => c >= 32 && c < 127) ? new string(chars).Trim() : $"0x{fourCC:X8}";
        }

        private static void FreeMediaType(IntPtr mediaTypePointer)
        {
            if (mediaTypePointer == IntPtr.Zero) return;

            var mediaType = Marshal.PtrToStructure<AMMediaType>(mediaTypePointer);
            if (mediaType.Format != IntPtr.Zero) Marshal.FreeCoTaskMem(mediaType.Format);
            if (mediaType.Unknown != IntPtr.Zero) Marshal.Release(mediaType.Unknown);
            Marshal.FreeCoTaskMem(mediaTypePointer);
        }

        #region DirectShow

        private static readonly Guid CLSID_SystemDeviceEnum = new Guid("62BE5D10-60EB-11d0-BD3B-00A0C911CE86");
        private static readonly Guid CLSID_VideoInputDeviceCategory = new Guid("860BB310-5D01-11d0-BD3B-00A0C911CE86");
        private static readonly Guid AMPROPSETID_Pin = new Guid("9B00F101-1567-11d1-B3F1-00AA003761C5");
        private static readonly Guid PIN_CATEGORY_CAPTURE = new Guid("fb6c4281-0353-11d1-905f-0000c0cc16ba");
        private static readonly Guid FORMAT_VideoInfo = new Guid("05589f80-c356-11ce-bf01-00aa0055595a");
        private static readonly Guid FORMAT_VideoInfo2 = new Guid("f72a76A0-eb0a-11d0-ace4-0000c0cc16ba");
        private static readonly byte[] FourCCGuidTail = new Guid("00000000-0000-0010-8000-00AA00389B71").ToByteArray().Skip(4).ToArray();
        private const int AMPROPERTY_PIN_CATEGORY = 0;
        private const int PINDIR_OUTPUT = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct AMMediaType
        {
            public Guid MajorType;
            public Guid SubType;
            public int FixedSizeSamples;
            public int TemporalCompression;
            public int SampleSize;
            public Guid FormatType;
            public IntPtr Unknown;
            public int FormatSize;
            public IntPtr Format;
        }

        [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICreateDevEnum
        {
            [PreserveSig] int CreateClassEnumerator([In] ref Guid category, out IEnumMoniker? enumerator, int flags);
        }

        [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyBag
        {
            [PreserveSig] int Read([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.Struct)] out object value, IntPtr errorLog);
        }

        // Only the methods up to the ones used, in vtable order.
        [ComImport, Guid("56a86895-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IBaseFilter
        {
            [PreserveSig] int GetClassID(out Guid classId);
            [PreserveSig] int Stop();
            [PreserveSig] int Pause();
            [PreserveSig] int Run(long start);
            [PreserveSig] int GetState(int timeout, out int state);
            [PreserveSig] int SetSyncSource(IntPtr clock);
            [PreserveSig] int GetSyncSource(out IntPtr clock);
            [PreserveSig] int EnumPins(out IEnumPins? pins);
        }

        [ComImport, Guid("56a86892-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IEnumPins
        {
            [PreserveSig] int Next(int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IPin[] pins, IntPtr fetched);
        }

        [ComImport, Guid("56a86891-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPin
        {
            [PreserveSig] int Connect(IntPtr receivePin, IntPtr mediaType);
            [PreserveSig] int ReceiveConnection(IntPtr connector, IntPtr mediaType);
            [PreserveSig] int Disconnect();
            [PreserveSig] int ConnectedTo(out IntPtr pin);
            [PreserveSig] int ConnectionMediaType(IntPtr mediaType);
            [PreserveSig] int QueryPinInfo(IntPtr info);
            [PreserveSig] int QueryDirection(out int direction);
        }

        [ComImport, Guid("31EFAC30-515C-11d0-A9AA-00AA0061BE93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IKsPropertySet
        {
            [PreserveSig] int Set(ref Guid propertySet, int id, IntPtr instance, int instanceLength, IntPtr data, int dataLength);
            [PreserveSig] int Get(ref Guid propertySet, int id, IntPtr instance, int instanceLength, out Guid data, int dataLength, out int returned);
        }

        [ComImport, Guid("C6E13340-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAMStreamConfig
        {
            [PreserveSig] int SetFormat(IntPtr mediaType);
            [PreserveSig] int GetFormat(out IntPtr mediaType);
            [PreserveSig] int GetNumberOfCapabilities(out int count, out int size);
            [PreserveSig] int GetStreamCaps(int index, out IntPtr mediaType, IntPtr caps);
        }

        [ComImport, Guid("6a2e0670-28e4-11d0-a18c-00a0c9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAMVideoControl
        {
            [PreserveSig] int GetCaps(IPin pin, out int flags);
            [PreserveSig] int SetMode(IPin pin, int mode);
            [PreserveSig] int GetMode(IPin pin, out int mode);
            [PreserveSig] int GetCurrentActualFrameRate(IPin pin, out long frameRate);
            [PreserveSig] int GetMaxAvailableFrameRate(IPin pin, int index, Size dimensions, out long frameRate);
            [PreserveSig] int GetFrameRateList(IPin pin, int index, Size dimensions, out int listSize, out IntPtr frameRates);
        }

        #endregion
    }
}

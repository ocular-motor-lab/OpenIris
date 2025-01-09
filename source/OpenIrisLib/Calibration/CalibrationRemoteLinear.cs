using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenIris.Calibration
{
#nullable enable

    /// <summary>
    /// Extensions to Emgu mat class that allow setting a single pixel. 
    /// From: https://stackoverflow.com/questions/32255440/how-can-i-get-and-set-pixel-values-of-an-emgucv-mat-image
    /// </summary>
    public static class MatExtension
    {
        public static dynamic GetValue(this Mat mat, int row, int col)
        {
            var value = CreateElement(mat.Depth);
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static void SetValue(this Mat mat, int row, int col, dynamic value)
        {
            var target = CreateElement(mat.Depth, value);
            Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
        }
        private static dynamic CreateElement(DepthType depthType, dynamic value)
        {
            var element = CreateElement(depthType);
            element[0] = value;
            return element;
        }

        private static dynamic CreateElement(DepthType depthType)
        {
            if (depthType == DepthType.Cv8S)
            {
                return new sbyte[1];
            }
            if (depthType == DepthType.Cv8U)
            {
                return new byte[1];
            }
            if (depthType == DepthType.Cv16S)
            {
                return new short[1];
            }
            if (depthType == DepthType.Cv16U)
            {
                return new ushort[1];
            }
            if (depthType == DepthType.Cv32S)
            {
                return new int[1];
            }
            if (depthType == DepthType.Cv32F)
            {
                return new float[1];
            }
            if (depthType == DepthType.Cv64F)
            {
                return new double[1];
            }
            return new float[1];
        }
    }

    /// <summary>
    /// An enum for differnet signal sources
    /// </summary>
    public enum EyeSignalSource
    {
        None = 0,
        Pupil,
        CR1,
        CR2,
        CR3,
        CR4
    }

    /// <summary>
    /// A class to wrap each series of fixation data. Holds the 
    /// point displayed to the participant, as well as all EyeData collected during the fixation
    /// </summary>

    class FixationData
    {
        public PointF fixation_point;
        public List<EyeData> fixation_data = new List<EyeData>();
        public FixationData() { }
        public FixationData(PointF fixation_point)
        {
            this.fixation_point = fixation_point;
        }

        /// <summary>
        /// Gets the median value from an array. From: https://stackoverflow.com/questions/4140719/calculate-median-in-c-sharp
        /// </summary>
        /// <typeparam name="T">The array type</typeparam>
        /// <param name="sourceArray">The source array</param>
        /// <param name="cloneArray">If it doesn't matter if the source array is sorted, you can pass false to improve performance</param>
        /// <returns></returns>
        public static T GetMedian<T>(T[] sourceArray, bool cloneArray = true) where T : IComparable<T>
        {
            //Framework 2.0 version of this method. there is an easier way in F4        
            if (sourceArray == null || sourceArray.Length == 0)
                throw new ArgumentException("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            T[] sortedArray = cloneArray ? (T[])sourceArray.Clone() : sourceArray;
            Array.Sort(sortedArray);

            //get the median
            int size = sortedArray.Length;
            int mid = size / 2;
            if (size % 2 != 0)
                return sortedArray[mid];

            dynamic value1 = sortedArray[mid];
            dynamic value2 = sortedArray[mid - 1];
            return (value1 + value2) / 2;
        }

        /// <summary>
        /// Gets the signal at an index given an eye of origin, signal source, and signal reference. Signal is computed as sig_src - sig_ref.
        /// </summary>
        /// <param name="idx">Index in array of EyeData</param>
        /// <param name="eye">Eye of origin</param>
        /// <param name="sig_src">Signal source</param>
        /// <param name="sig_ref">Signal reference</param>
        /// <returns>A PointF representing the signal in pixels. Null if eye is incorrect or out of bounds.</returns>
        public PointF? GetEyeSignalAtIdx(int idx, Eye eye, EyeSignalSource sig_src, EyeSignalSource sig_ref)
        {
            if (fixation_data.Count < idx) return null;
            var eye_data = fixation_data[idx];
            if (eye_data.WhichEye != eye) return null;

            var crs = eye_data.CornealReflections;
            PointF data_src = new PointF(), data_ref = new PointF();

            switch (sig_src)
            {
                case EyeSignalSource.None:
                    data_src = new PointF(0.0f, 0.0f);
                    break;
                case EyeSignalSource.Pupil:
                    data_src = eye_data.Pupil.Center;
                    break;
                case EyeSignalSource.CR1:
                    if (crs != null && crs.Length > 0)
                    {
                        data_src = crs[0].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case EyeSignalSource.CR2:
                    if (crs != null && crs.Length > 1)
                    {
                        data_src = crs[1].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case EyeSignalSource.CR3:
                    if (crs != null && crs.Length > 2)
                    {
                        data_src = crs[2].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case EyeSignalSource.CR4:
                    if (crs != null && crs.Length > 3)
                    {
                        data_src = crs[3].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
            }

            switch (sig_ref)
            {
                case EyeSignalSource.None:
                    data_ref = new PointF(0.0f, 0.0f);
                    break;
                case EyeSignalSource.Pupil:
                    data_ref = eye_data.Pupil.Center;
                    break;
                case EyeSignalSource.CR1:
                    if (crs != null && crs.Length > 0)
                    {
                        data_ref = crs[0].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case EyeSignalSource.CR2:
                    if (crs != null && crs.Length > 1)
                    {
                        data_ref = crs[1].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case EyeSignalSource.CR3:
                    if (crs != null && crs.Length > 2)
                    {
                        data_ref = crs[2].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case EyeSignalSource.CR4:
                    if (crs != null && crs.Length > 3)
                    {
                        data_ref = crs[3].Center;
                    }
                    else
                    {
                        return null;
                    }
                    break;
            }

            return new PointF(data_src.X - data_ref.X, data_src.Y - data_ref.Y);
        }

        /// <summary>
        /// Gets the median signal for this fixation class as a single PointF. Signal is defined as sig_src - sig_ref.
        /// </summary>
        /// <param name="eye">Specifies the eye</param>
        /// <param name="sig_src">Signal source</param>
        /// <param name="sig_ref">Signal Reference</param>
        /// <returns>PointF defined as sig_src - sig_ref. Null if no EyeData exists.</returns>
        public PointF? GetMedianData(Eye eye, EyeSignalSource sig_src, EyeSignalSource sig_ref)
        {
            if (sig_src == EyeSignalSource.None && sig_ref == EyeSignalSource.None) return new PointF(0.0f, 0.0f);
            var x = new List<float>(fixation_data.Count);
            var y = new List<float>(fixation_data.Count);

            for (int i = 0; i < fixation_data.Count; i++)
            {
                PointF? signal = GetEyeSignalAtIdx(i, eye, sig_src, sig_ref);
                if (signal != null)
                {
                    x.Add(signal.Value.X);
                    y.Add(signal.Value.Y);
                }

            }

            if (x.Count > 0 && y.Count > 0)
            {
                return new PointF(GetMedian<float>(x.ToArray(), false), GetMedian<float>(y.ToArray(), false));
            }
            else
            {
                return null;
            }

        }
    }

    /// <summary>
    /// A class to hold calibration data, as well as keep track of state and compute a linear calibration.
    /// </summary>
    class LinearCalibrator
    {
        public List<FixationData> calibration_data = new List<FixationData>();
        public bool in_fixation = false;
        public bool completed = false;
        public int n_data_points = 0;

        public LinearCalibrator() { }

        /// <summary>
        /// Adds EyeData to the currently active FixationData if currently in a fixation.
        /// </summary>
        /// <param name="data">A list of EyeData to append to the current fixation</param>
        public void PushData(List<EyeData> data)
        {
            if (calibration_data.Count > 0 && in_fixation)
            {
                calibration_data[calibration_data.Count - 1].fixation_data.AddRange(data);
                n_data_points += data.Count;
            }
        }

        /// <summary>
        /// Adds EyeData to the currently active FixationData if currently in a fixation.
        /// </summary>
        /// <param name="data">EyeData to append to the current fixation</param>
        public void PushData(EyeData data)
        {
            if (calibration_data.Count > 0 && in_fixation)
            {
                calibration_data[calibration_data.Count - 1].fixation_data.Add(data);
                n_data_points++;
            }
        }

        /// <summary>
        /// Starts a fixation.
        /// </summary>
        /// <param name="fixation_point">Ground truth fixation point presented to participant</param>
        public void StartFixation(PointF fixation_point)
        {
            calibration_data.Add(new FixationData(fixation_point));
            in_fixation = true;
        }

        /// <summary>
        /// Aborts the currently active fixation, removing its data so it is not counted in the final calibration.
        /// </summary>
        public void AbortFixation()
        {
            if (calibration_data.Count > 0 && in_fixation)
            {
                calibration_data.RemoveAt(calibration_data.Count - 1);
                in_fixation = false;
            }
        }

        /// <summary>
        /// Ends the current fixation, saving the data to be used in the final calibration.
        /// </summary>
        public void StopFixation()
        {
            if (calibration_data.Count > 0 && in_fixation)
            {
                in_fixation = false;
            }
        }

        /// <summary>
        /// Completes the calibration routine and updates the plugins settings to store the results.
        /// </summary>
        /// <param name="settings">A reference to the calibration settings</param>
        public void CompleteCalibration(ref CalibrationSettingsRemoteLinear settings)
        {
            // Loop over fixation datasets and fill lists containing target points (Y) and eye data (X) 
            var left_data = new List<PointF>(calibration_data.Count);
            var left_target = new List<PointF>(calibration_data.Count);

            var right_data = new List<PointF>(calibration_data.Count);
            var right_target = new List<PointF>(calibration_data.Count);
            for (var i = 0; i < calibration_data.Count; i++)
            {
                PointF? left_pt = calibration_data[i].GetMedianData(Eye.Left, settings.SignalSource, settings.SignalReference);
                if (left_pt != null)
                {
                    left_data.Add(left_pt.Value);
                    left_target.Add(calibration_data[i].fixation_point);
                }

                PointF? right_pt = calibration_data[i].GetMedianData(Eye.Right, settings.SignalSource, settings.SignalReference);
                if (right_pt != null)
                {
                    right_data.Add(right_pt.Value);
                    right_target.Add(calibration_data[i].fixation_point);
                }
            }

            // Create matrices for the regression and target datasets. Solve using CvInvoke.Solve
            var X_left = Mat.Ones(left_data.Count, 3, DepthType.Cv32F, 1);
            var Y_left = Mat.Ones(left_data.Count, 2, DepthType.Cv32F, 1);

            for (var i = 0; i < left_data.Count; i++)
            {
                X_left.SetValue(i, 0, left_data[i].X);
                X_left.SetValue(i, 1, left_data[i].Y);
                Y_left.SetValue(i, 0, left_target[i].X);
                Y_left.SetValue(i, 1, left_target[i].Y);
            }

            var left_cal = new Mat();
            var left_ret = CvInvoke.Solve(X_left, Y_left, left_cal, DecompMethod.Svd);

            if (left_ret != 0)
            {
                settings.LeftGxx = left_cal.GetValue(0, 0);
                settings.LeftGxy = left_cal.GetValue(0, 1);
                settings.LeftGyx = left_cal.GetValue(1, 0);
                settings.LeftGyy = left_cal.GetValue(1, 1);

                settings.LeftXOffset = left_cal.GetValue(2, 0);
                settings.LeftYOffset = left_cal.GetValue(2, 1);
            }
            else
            {
                Console.WriteLine("Calibration error: Left data was singular");
            }

            var X_right = Mat.Ones(right_data.Count, 3, DepthType.Cv32F, 1);
            var Y_right = Mat.Ones(right_data.Count, 2, DepthType.Cv32F, 1);

            for (var i = 0; i < right_data.Count; i++)
            {
                X_right.SetValue(i, 0, right_data[i].X);
                X_right.SetValue(i, 1, right_data[i].Y);
                Y_right.SetValue(i, 0, right_target[i].X);
                Y_right.SetValue(i, 1, right_target[i].Y);
            }

            var right_cal = new Mat();
            var right_ret = CvInvoke.Solve(X_right, Y_right, right_cal, DecompMethod.Svd);

            if (right_ret != 0)
            {
                settings.RightGxx = right_cal.GetValue(0, 0);
                settings.RightGxy = right_cal.GetValue(0, 1);
                settings.RightGyx = right_cal.GetValue(1, 0);
                settings.RightGyy = right_cal.GetValue(1, 1);

                settings.RightXOffset = right_cal.GetValue(2, 0);
                settings.RightYOffset = right_cal.GetValue(2, 1);
            }
            else
            {
                Console.WriteLine("Calibration error: Right data was singular");
            }

        }

        /// <summary>
        /// Clear all fixation data
        /// </summary>
        public void Reset()
        {
            calibration_data.Clear();
        }

        /// <summary>
        /// Get data for UI
        /// </summary>
        /// <param name="sig_src">Signal Source</param>
        /// <param name="sig_ref">Signal Reference</param>
        /// <returns></returns>
        public List<FixationGridViewRow> GetUIData(EyeSignalSource sig_src, EyeSignalSource sig_ref)
        {
            var data = new List<FixationGridViewRow>();
            for (int i = 0; i < calibration_data.Count; i++)
            {
                var row = calibration_data[i];
                var left_median = row.GetMedianData(Eye.Left, sig_src, sig_ref);
                var right_median = row.GetMedianData(Eye.Right, sig_src, sig_ref);

                var element = new FixationGridViewRow()
                {
                    FixNum = i,
                    FixX = row.fixation_point.X,
                    FixY = row.fixation_point.Y,
                    LeftX = left_median?.X ?? null,
                    LeftY = left_median?.Y ?? null,
                    RightX = right_median?.X ?? null,
                    RightY = right_median?.Y ?? null
                };

                data.Add(element);
            }
            return data;
        }
    }

    [Export(typeof(CalibrationPipelineBase)), PluginDescription("Remote Linear", typeof(CalibrationSettingsRemoteLinear))]
    public class CalibrationPipelineRemoteLinear : CalibrationPipelineBase
    {
        private CalibrationRemoteLinearUI? ui;
        private LinearCalibrator calibrator = new LinearCalibrator();


        /// <summary>
        /// User interface of the calibration.
        /// </summary>
        public override ICalibrationUIControl? GetCalibrationUI()
        {
            return ui;
        }

        /// <summary>
        /// Pushes EyeData to the calibrator 
        /// </summary>
        public override (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(ImageEye image, EyeTrackingPipelineSettings processingSettings)
        {
            if (ui is null)
            {
                ui = new CalibrationRemoteLinearUI();
            }

            if (!(image.EyeData is null))
            {
                calibrator.PushData(image.EyeData);
            }

            return (calibrator.completed, calibrator.completed ? EyePhysicalModel.EmptyModel : EyePhysicalModel.EmptyModel);
        }

        /// <summary>
        /// Reference is implicit in the fixation data (0,0), so just return once called
        /// </summary>
        public override (bool referenceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(ImageEye image, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
        {
            ui = null; // No UI for resetting reference

            if (image == null) return (false, null);

            if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

            return (true, image);
        }

        /// <summary>
        /// Handles incoming messages from the client
        /// 
        /// Messages: 
        /// SetSource:n  -> Sets SignalSource     (0=None, 1=Pupil, 2=CR1, 3=CR2, 4=CR3, 5=CR4)
        /// SetRef:n     -> Sets SignalReference  (0=None, 1=Pupil, 2=CR1, 3=CR2, 4=CR3, 5=CR4)
        /// FixStart:x,y -> Start collecting data for a fixation, with the ground-truth point at x,y
        /// FixStop      -> Finish collecting data for the previous fixation
        /// FixAbort     -> Finish collecting data and discard the previous fixation
        /// Complete     -> Conclude calibration and calculate linear tranform from fixation points to x,y
        /// 
        /// Note: Each fixation block has its median taken and is counted as a single point for regression
        /// </summary>
        /// <param name="message">Message from client</param>
        /// <returns>"Ack" or error to the client</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override string HandleRemoteMessage(string message)
        {
            if (message.StartsWith("SetSource:", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    var settings = Settings as CalibrationSettingsRemoteLinear ?? throw new ArgumentNullException(nameof(Settings));
                    settings.SignalSource = (EyeSignalSource)Int32.Parse(message.Substring("SetSource:".Length));
                }
                catch (Exception ex)
                {
                    return $"Unable to parse '{message}' as SetSource command: {ex.Message}";
                }
                return "ack";
            }
            else if (message.StartsWith("SetRef:", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    var settings = Settings as CalibrationSettingsRemoteLinear ?? throw new ArgumentNullException(nameof(Settings));
                    settings.SignalReference = (EyeSignalSource)Int32.Parse(message.Substring("SetRef:".Length));
                }
                catch (Exception ex)
                {
                    return $"Unable to parse '{message}' as SetRef command: {ex.Message}";
                }
                return "ack";
            }
            else if (message.StartsWith("FixStart:", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    var pt = message.Substring("FixStart:".Length).Split(',');
                    if (pt.Length != 2)
                        throw new Exception("Must be two fields in FixStart command");

                    var x = float.Parse(pt[0]);
                    var y = float.Parse(pt[1]);
                    calibrator.StartFixation(new PointF(x, y));

                    if (ui != null)
                        ui.status = calibrator.in_fixation ? "Fixating" : "Waiting";
                }
                catch (Exception ex)
                {
                    return $"Unable to parse '{message}' as FixStart command: {ex.Message}";
                }
                return "ack";
            }
            else if (message.Equals("FixStop", StringComparison.InvariantCultureIgnoreCase))
            {
                var settings = Settings as CalibrationSettingsRemoteLinear ?? throw new ArgumentNullException(nameof(Settings));
                calibrator.StopFixation();
                if (ui != null)
                {
                    ui.fix_data = calibrator.GetUIData(settings.SignalSource, settings.SignalReference);
                    ui.status = calibrator.in_fixation ? "Fixating" : "Waiting";
                }
                return "ack";
            }
            else if (message.Equals("FixAbort", StringComparison.InvariantCultureIgnoreCase))
            {
                calibrator.AbortFixation();
                if (ui != null)
                {
                    ui.status = calibrator.in_fixation ? "Fixating" : "Waiting";
                }
                return "ack";
            }
            else if (message.Equals("Complete", StringComparison.InvariantCultureIgnoreCase))
            {
                var settings = Settings as CalibrationSettingsRemoteLinear ?? throw new ArgumentNullException(nameof(Settings));
                calibrator.CompleteCalibration(ref settings);
                calibrator.completed = true;
                return "ack";
            }
            else
            {
                return $"ERROR: Unknown Command '{message}'";
            }
        }

    }


    [Serializable]
    public class CalibrationSettingsRemoteLinear : CalibrationSettings
    {

        [Category("Sources"), Description("Source of eye position.")]
        public EyeSignalSource SignalSource { get => signalSource; set => SetProperty(ref signalSource, value, nameof(SignalSource)); }
        private EyeSignalSource signalSource = EyeSignalSource.Pupil;

        [Category("Sources"), Description("Source of eye reference.")]
        public EyeSignalSource SignalReference { get => signalReference; set => SetProperty(ref signalReference, value, nameof(SignalReference)); }
        private EyeSignalSource signalReference = EyeSignalSource.CR1;

        [Category("Parameters"), Description("Offset in X (Left Eye)")]
        public double LeftXOffset { get => leftXOffset; set => SetProperty(ref leftXOffset, value, nameof(LeftXOffset)); }
        private double leftXOffset = 0.0;

        [Category("Parameters"), Description("Offset in Y (Left Eye)")]
        public double LeftYOffset { get => leftYOffset; set => SetProperty(ref leftYOffset, value, nameof(LeftYOffset)); }
        private double leftYOffset = 0.0;

        [Category("Parameters"), Description("Gain Gxx (Left Eye)")]
        public double LeftGxx { get => leftGxx; set => SetProperty(ref leftGxx, value, nameof(LeftGxx)); }
        private double leftGxx = 1.0;

        [Category("Parameters"), Description("Gain Gxy (Left Eye)")]
        public double LeftGxy { get => leftGxy; set => SetProperty(ref leftGxy, value, nameof(LeftGxy)); }
        private double leftGxy = 1.0;

        [Category("Parameters"), Description("Gain Gyx (Left Eye)")]
        public double LeftGyx { get => leftGyx; set => SetProperty(ref leftGyx, value, nameof(LeftGyx)); }
        private double leftGyx = 1.0;

        [Category("Parameters"), Description("Gain Gyy (Left Eye)")]
        public double LeftGyy { get => leftGyy; set => SetProperty(ref leftGyy, value, nameof(LeftGyy)); }
        private double leftGyy = 1.0;


        [Category("Parameters"), Description("Offset in X (Right Eye)")]
        public double RightXOffset { get => rightXOffset; set => SetProperty(ref rightXOffset, value, nameof(RightXOffset)); }
        private double rightXOffset = 0.0;

        [Category("Parameters"), Description("Offset in Y (Right Eye)")]
        public double RightYOffset { get => rightYOffset; set => SetProperty(ref rightYOffset, value, nameof(RightYOffset)); }
        private double rightYOffset = 0.0;

        [Category("Parameters"), Description("Gain Gxx (Right Eye)")]
        public double RightGxx { get => rightGxx; set => SetProperty(ref rightGxx, value, nameof(RightGxx)); }
        private double rightGxx = 1.0;

        [Category("Parameters"), Description("Gain Gxy (Right Eye)")]
        public double RightGxy { get => rightGxy; set => SetProperty(ref rightGxy, value, nameof(RightGxy)); }
        private double rightGxy = 1.0;

        [Category("Parameters"), Description("Gain Gyx (Right Eye)")]
        public double RightGyx { get => rightGyx; set => SetProperty(ref rightGyx, value, nameof(RightGyx)); }
        private double rightGyx = 1.0;

        [Category("Parameters"), Description("Gain Gyy (Right Eye)")]
        public double RightGyy { get => rightGyy; set => SetProperty(ref rightGyy, value, nameof(RightGyy)); }
        private double rightGyy = 1.0;
    }
}

using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Diagnostics;
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
        CR4,
        CR5
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
            if (fixation_data.Count <= idx) return null;
            var eye_data = fixation_data[idx];
            if (eye_data.WhichEye != eye) return null;

            // The same signal as when the map is applied to the data.
            return LinearGazeMap.Signal(eye_data, sig_src, sig_ref);
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
        /// The map fitted for each eye, which goes into the calibration and is applied to the data.
        /// Null for an eye whose fit failed.
        /// </summary>
        public EyeCollection<LinearGazeMap?> Maps { get; } = new EyeCollection<LinearGazeMap?>(null, null);

        /// <summary>
        /// Completes the calibration: fits the map of each eye from the median of each fixation.
        /// </summary>
        /// <param name="source">Signal source.</param>
        /// <param name="reference">Signal reference.</param>
        public void CompleteCalibration(EyeSignalSource source, EyeSignalSource reference)
        {
            foreach (var eye in new[] { Eye.Left, Eye.Right })
            {
                Maps[eye] = Fit(eye, source, reference);
                if (Maps[eye] is null) Trace.WriteLine($"N-point calibration: no map for the {eye.ToString().ToLower()} eye (fewer than 3 targets with data, or the data was singular).");
            }
        }

        private LinearGazeMap? Fit(Eye eye, EyeSignalSource source, EyeSignalSource reference)
        {
            // The median signal and the target of each fixation with data (Y = X * map).
            var data = new List<PointF>(calibration_data.Count);
            var targets = new List<PointF>(calibration_data.Count);
            foreach (var fixation in calibration_data)
            {
                if (fixation.GetMedianData(eye, source, reference) is PointF point)
                {
                    data.Add(point);
                    targets.Add(fixation.fixation_point);
                }
            }

            // Three unknowns per axis: an eye with fewer fixations (or none: a system with one eye) gets no map.
            if (data.Count < 3) return null;

            using var x = Mat.Ones(data.Count, 3, DepthType.Cv32F, 1);
            using var y = Mat.Ones(data.Count, 2, DepthType.Cv32F, 1);
            for (var i = 0; i < data.Count; i++)
            {
                x.SetValue(i, 0, data[i].X);
                x.SetValue(i, 1, data[i].Y);
                y.SetValue(i, 0, targets[i].X);
                y.SetValue(i, 1, targets[i].Y);
            }

            using var map = new Mat();
            if (!CvInvoke.Solve(x, y, map, DecompMethod.Svd)) return null;

            return new LinearGazeMap
            {
                Gxx = map.GetValue(0, 0),
                Gxy = map.GetValue(0, 1),
                Gyx = map.GetValue(1, 0),
                Gyy = map.GetValue(1, 1),
                XOffset = map.GetValue(2, 0),
                YOffset = map.GetValue(2, 1),
                Source = source,
                Reference = reference,
            };
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

    /// <summary>
    /// Who shows the targets of the N-point calibration and decides where and when.
    /// </summary>
    public enum NPointMode
    {
        /// <summary>OpenIris shows the targets itself, full screen on a display of its computer (StimulusDisplay).</summary>
        Local,

        /// <summary>OpenIris decides the targets, and a display client draws them (the web client's display page, on another computer).</summary>
        RemoteDisplay,

        /// <summary>A remote program shows the targets and tells OpenIris with messages (FixStart:x,y, FixStop, FixAbort, Complete).</summary>
        Remote,
    }

    /// <summary>
    /// N-point calibration (called Remote Linear before 0.2): for each eye, a linear map from the eye
    /// signal (pupil minus a reflection, by default) to the targets, in degrees. The targets are shown
    /// by OpenIris (Local), by a display client that OpenIris controls (RemoteDisplay), or by a remote
    /// program that controls them (Remote). See docs/user-manual/calibration.md.
    /// </summary>
    [Export(typeof(CalibrationPipelineBase)), PluginDescription(MethodName, typeof(CalibrationSettingsRemoteLinear))]
    public class CalibrationPipelineRemoteLinear : CalibrationPipelineBase
    {
        /// <summary>Name of the calibration method.</summary>
        public const string MethodName = "N-point";

        private LinearCalibrator calibrator = new LinearCalibrator();

        // The targets when OpenIris controls them (Local and RemoteDisplay).
        private NPointSequence? sequence;
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private int recordingNumber;

        /// <summary>
        /// Seconds since the targets started, for the tests. The time of the computer if null.
        /// </summary>
        internal Func<double>? Clock { get; set; }

        /// <summary>
        /// Gets who shows the targets.
        /// </summary>
        public NPointMode Mode => (Settings as CalibrationSettingsRemoteLinear)?.Mode ?? NPointMode.Remote;

        /// <summary>
        /// Gets the target to draw now when OpenIris controls the targets (Local and RemoteDisplay
        /// modes), or null. The center target while the reference is taken, after the targets.
        /// </summary>
        public NPointTarget? Target { get; private set; }

        /// <summary>
        /// Stops the calibration, for example when the subject presses Esc on the targets.
        /// </summary>
        public void Cancel() => Cancelled = true;

        /// <summary>
        /// Whether the calibration is collecting a fixation ("Fixating") or not ("Waiting"), for the
        /// user interface.
        /// </summary>
        public string Status { get; private set; } = "Waiting";

        /// <summary>
        /// The fixations collected so far, for the user interface.
        /// </summary>
        public List<FixationGridViewRow> Fixations { get; private set; } = new List<FixationGridViewRow>();

        /// <summary>
        /// Gives the data of each eye to the calibrator while a fixation is open, until the client sends
        /// Complete; then the maps fitted for each eye (the calibrated positions come from them). The eye
        /// models are left to the session for now (from the reference frame).
        /// </summary>
        public override (bool completed, CalibrationParameters? calibration) ProcessForCalibrationModel(EyeTrackerImagesAndData frame, EyeTrackingPipelineSettings processingSettings)
        {
            if (Mode != NPointMode.Remote) RunTargets();

            foreach (var eye in Eyes(EyesToCalibrate))
            {
                if (frame.Images[eye]?.EyeData is EyeData eyeData) calibrator.PushData(eyeData);
            }

            if (!calibrator.completed) return (false, null);

            var calibration = new CalibrationParameters();
            foreach (var eye in Eyes(EyesToCalibrate))
            {
                calibration.EyeCalibrationParameters[eye].CalibrationData = calibrator.Maps[eye];
            }

            return (true, calibration);
        }

        /// <summary>
        /// The references (the torsion needs one), from the first frame with every eye tracked.
        /// </summary>
        public override (bool completed, CalibrationParameters? calibration) ProcessForZeroReference(EyeTrackerImagesAndData frame, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
        {
            // The subject looks at the center while the reference is taken.
            if (Mode != NPointMode.Remote) Target = new NPointTarget(new PointF(0, 0), 1, true, 0, 0);

            if (!AllEyesTracked(frame)) return (false, null);

            return (true, WithReferences(currentCalibration, frame));
        }

        /// <summary>
        /// Shows the targets when OpenIris controls them: starts and stops collecting each one as the
        /// sequence goes, and completes the calibration after the last.
        /// </summary>
        private void RunTargets()
        {
            var settings = (CalibrationSettingsRemoteLinear)Settings;
            if (sequence is null)
            {
                var points = NPointSequence.Layout((int)settings.Targets, (float)settings.TargetsHorizontal, (float)settings.TargetsVertical, new Random());
                sequence = new NPointSequence(points, settings.SettleSeconds, settings.CollectSeconds);
                stopwatch.Restart();
            }

            if (calibrator.completed) return;

            var target = sequence.TargetAt(Clock?.Invoke() ?? stopwatch.Elapsed.TotalSeconds);

            // A target's data ends when the next target starts moving, or after the last.
            if (recordingNumber != 0 && target?.Number != recordingNumber)
            {
                calibrator.StopFixation();
                Fixations = calibrator.GetUIData(settings.SignalSource, settings.SignalReference);
                recordingNumber = 0;
            }

            if (target is not null && target.Recording && recordingNumber == 0)
            {
                calibrator.StartFixation(target.Position);
                recordingNumber = target.Number;
            }

            Target = target;
            Status = target is null ? "Done" : $"Target {target.Number} of {target.Count}" + (target.Recording ? ", collecting" : string.Empty);

            if (target is null)
            {
                calibrator.CompleteCalibration(settings.SignalSource, settings.SignalReference);
                calibrator.completed = true;
            }
        }

        /// <summary>
        /// Handles incoming messages from the client
        /// 
        /// Messages: 
        /// SetSource:n  -> Sets SignalSource     (0=None, 1=Pupil, 2=CR1, 3=CR2, 4=CR3, 5=CR4, 6=CR5)
        /// SetRef:n     -> Sets SignalReference  (0=None, 1=Pupil, 2=CR1, 3=CR2, 4=CR3, 5=CR4, 6=CR5)
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
        public override string HandleMessage(string message)
        {
            // When OpenIris shows the targets, the targets don't come in messages.
            if (Mode != NPointMode.Remote && !message.StartsWith("Set", StringComparison.InvariantCultureIgnoreCase))
            {
                return $"ERROR: OpenIris shows the targets (N-point calibration in the {Mode} mode). Set Mode to Remote to send them.";
            }

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

                    Status = calibrator.in_fixation ? "Fixating" : "Waiting";
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
                Fixations = calibrator.GetUIData(settings.SignalSource, settings.SignalReference);
                Status = calibrator.in_fixation ? "Fixating" : "Waiting";
                return "ack";
            }
            else if (message.Equals("FixAbort", StringComparison.InvariantCultureIgnoreCase))
            {
                calibrator.AbortFixation();
                Status = calibrator.in_fixation ? "Fixating" : "Waiting";
                return "ack";
            }
            else if (message.Equals("Complete", StringComparison.InvariantCultureIgnoreCase))
            {
                var settings = Settings as CalibrationSettingsRemoteLinear ?? throw new ArgumentNullException(nameof(Settings));
                calibrator.CompleteCalibration(settings.SignalSource, settings.SignalReference);
                calibrator.completed = true;
                return "ack";
            }
            else
            {
                return $"ERROR: Unknown Command '{message}'";
            }
        }

    }


    /// <summary>
    /// How many targets the N-point calibration shows when OpenIris controls them.
    /// </summary>
    public enum NPointTargets
    {
        /// <summary>The center and the four corners.</summary>
        Five = 5,

        /// <summary>A 3 × 3 grid.</summary>
        Nine = 9,

        /// <summary>The 3 × 3 grid and the corners of a grid half as big.</summary>
        Thirteen = 13,
    }

    [Serializable]
    public class CalibrationSettingsRemoteLinear : CalibrationSettings
    {
        [Category("Targets"), Description("Who shows the targets: Local, OpenIris itself on the display StimulusDisplay of the eye tracking system; " +
            "RemoteDisplay, a display client that OpenIris controls (the web client's display page); Remote, a remote program that shows them and sends " +
            "FixStart:x,y, FixStop and Complete. The screen's size and distance are settings of the eye tracking system.")]
        public NPointMode Mode { get => mode; set => SetProperty(ref mode, value, nameof(Mode)); }
        private NPointMode mode = NPointMode.Remote;

        [Category("Targets"), Description("How many targets OpenIris shows (Local and RemoteDisplay modes).")]
        public NPointTargets Targets { get => targets; set => SetProperty(ref targets, value, nameof(Targets)); }
        private NPointTargets targets = NPointTargets.Nine;

        [Category("Targets"), Description("Horizontal distance from the center to the outer targets, in degrees.")]
        public double TargetsHorizontal { get => targetsHorizontal; set => SetProperty(ref targetsHorizontal, value, nameof(TargetsHorizontal)); }
        private double targetsHorizontal = 10;

        [Category("Targets"), Description("Vertical distance from the center to the outer targets, in degrees.")]
        public double TargetsVertical { get => targetsVertical; set => SetProperty(ref targetsVertical, value, nameof(TargetsVertical)); }
        private double targetsVertical = 8;

        [Category("Targets"), Description("Diameter of the targets, in degrees.")]
        public double TargetSize { get => targetSize; set => SetProperty(ref targetSize, value, nameof(TargetSize)); }
        private double targetSize = 0.8;

        [Category("Targets"), Description("Time for the eye to settle on each target before its data is collected, in seconds.")]
        public double SettleSeconds { get => settleSeconds; set => SetProperty(ref settleSeconds, value, nameof(SettleSeconds)); }
        private double settleSeconds = 0.8;

        [Category("Targets"), Description("Time collecting the data of each target, in seconds.")]
        public double CollectSeconds { get => collectSeconds; set => SetProperty(ref collectSeconds, value, nameof(CollectSeconds)); }
        private double collectSeconds = 1.0;

        [Category("Sources"), Description("Source of eye position.")]
        public EyeSignalSource SignalSource { get => signalSource; set => SetProperty(ref signalSource, value, nameof(SignalSource)); }
        private EyeSignalSource signalSource = EyeSignalSource.Pupil;

        [Category("Sources"), Description("Source of eye reference.")]
        public EyeSignalSource SignalReference { get => signalReference; set => SetProperty(ref signalReference, value, nameof(SignalReference)); }
        private EyeSignalSource signalReference = EyeSignalSource.CR1;
    }

    /// <summary>
    /// One fixation of the remote linear calibration, as shown in the user interface.
    /// </summary>
    public class FixationGridViewRow
    {
        public int FixNum { get; set; }
        public float FixX { get; set; }
        public float FixY { get; set; }
        public float? LeftX { get; set; }
        public float? LeftY { get; set; }
        public float? RightX { get; set; }
        public float? RightY { get; set; }
    }
}

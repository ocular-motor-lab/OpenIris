using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using System.Windows.Forms;
using OpenIris;
using OpenIris.ImageGrabbing;
using OpenIris.ImageProcessing;

namespace SimpleEyeTrackerUI
{
    public partial class Form1 : Form
    {
        private string fileName;
        private VideoEye video;

        private EyeTrackingPipelineJOM imageProcessor;
        private EyeCalibration calibration;
        private EyeTrackingPipelineJOMSettings settings;

        private Timer timer;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            //======================================================================================
            // Initialize video capture.
            //======================================================================================
            this.fileName = "JorgeLeft.avi";
            this.video = new VideoEye(Eye.Left, this.fileName);

            //======================================================================================
            // Initialize eye tracker.
            //======================================================================================
            EyeTrackerPluginManager.Init(true);
            this.imageProcessor = new EyeTrackingPipelineJOM();

            //======================================================================================
            // Initialize settings.
            //======================================================================================

            // Some can go with default values. Others need to be set depending on the resolution and
            // brightnessof the image
            this.settings = new EyeTrackingPipelineJOMSettings
            {

                // Select the specific algorithms to be used for each processing step.

                // This is the rough finding of the pupil. Options are: "Blob" and "Centroid"
                PupilTrackingMethod = PupilTracking.PupilTrackingMethod.Blob,
                // This is the eyelid tracking method. Options are: "None", "Fixed" and "HoughLines".
                EyelidTrackingMethod = EyeLidTracking.EyeLidTrackingMethod.Fixed,
                // This is the precise pupil position calculation with subpixel resolution. Options are :
                // "None", "Centroid", "ConvexHull" and "EllipseFitting"
                PositionTrackingMethod = PositionTrackerEllipseFitting.PositionTrackingMethod.EllipseFitting,

                // Optional ROI in pixels. It does not represent the rectangle coordinates. It represents
                // how many pixels to ignore from top, left, bottom, right.
                CroppingLeftEye = new Rectangle(0, 0, 0, 0),

                // Thresholds for detecting the pupil (dark) and reflections (bright)
                BrightThresholdLeftEye = 250,
                DarkThresholdLeftEye = 50,

                // Radious of the iris. Manually set. Critical for torsion calclation. It does not
                // actually change much from subject to subject if using the same eye tracker.
                IrisRadiusPixLeft = 80,

                // mm per pixels in the image. Not critical for tracking to have a very precise value,
                // just ballpark. Only used to transform minimum and maximum sizes from mm to pix that
                // way only one setting needs to be changed when changing the eye tracker system.
                MmPerPix = 0.15,

                // Minimum size of the pupil in mm. Useful when identifying blobs.
                MinPupRadmm = 1,
                // Maximum iris radius in mm
                MaxIrisRadmm = 15,
                // Maximum torsion allowed
                MaxTorsion = 25
            };

            //======================================================================================
            // Initialize calibration.
            //======================================================================================

            // The calibration has two parts. 1) the eye physical model. Which asumes espheric eye
            // globe with no distorsion. So the only parameters are radius and center (in pixels of
            // the video frame). 2) the reference image for torision and reference (zero) eye
            // position. For that it is necessary to process at least one image.
            this.calibration = new EyeCalibration(Eye.Left);
            this.calibration.SetEyeModel(new EyePhysicalModel(new PointF(175, 100), 160));

            var imageEye = this.video.GrabImageEye();
            (imageEye.EyeData, imageEye.ImageTorsion) = this.imageProcessor.Process(imageEye, this.calibration, this.settings);

            // Get the torsion reference image
            this.calibration.SetReference(imageEye);

            // Start UI updating timer
            this.timer = new Timer();
            this.timer.Interval = 30;
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        private void Timer_Tick(object sender, System.EventArgs e)
        {
            // Get the image from the video
            var rawImageEye = this.video.GrabImageEye();

            //======================================================================================
            // Processing: Option 1 complete processing
            //======================================================================================

            // Complete procesing of the image, pupil, eyelids, torsion.The object processedImageEye
            // will contain the raw image but also the data obtained after the processing
            (rawImageEye.EyeData, rawImageEye.ImageTorsion) = this.imageProcessor.Process(rawImageEye, this.calibration, this.settings);

            //======================================================================================
            // Processing: Option 2 only eyelids, mask and torsion (asuming pupil position has been
            // pre -calculated
            //======================================================================================

            // Find the eyelids
            var eyelidMethod = new EyeLidTracking(); // Or any other class that implements IEyelidTracker interface
            var eyelids = eyelidMethod.FindEyelids(rawImageEye, rawImageEye.EyeData.Pupil, this.calibration.EyePhysicalModel, this.settings);

            // Create the mask for pixels that are valid (within the eyelids and not in a bright reflection)
            var maskCalculator = new EyeTrackerMask();
            var mask = maskCalculator.GetMask(rawImageEye, eyelids, this.calibration.EyePhysicalModel, this.settings);

            // Calculate the torsion angle
            Image<Gray, byte> imageTorsion = null;  // Image of the optimized iris pattern
            double dataQuality = 0.0;               // An index of data quality related to the autocorrelation of the iris pattern
                                                    // (Not very tested)
            var torsionMethod = new TorsionTracking();
            var torsionAngle = torsionMethod.CalculateTorsionAngle(
                rawImageEye,                                // Raw image
                this.calibration.EyePhysicalModel,          // Parameters of the eye globe in the image
                this.calibration.ImageTorsionReference,     // Iris pattern from the reference image
                mask,                                       // Image masking pixels that are not valid iris
                rawImageEye.EyeData.Pupil,                              // Pupil ellipse description (center, diameters, angle)
                rawImageEye.EyeData.Iris,                               // Iris ellipse description (center, radius)
                this.settings,                              // Current tracking settings
                out imageTorsion,                           // Output current torsion image
                out dataQuality                             // Output data quality index
                );

            //======================================================================================
            // Processing: Option 3 only torsion
            //======================================================================================

            // In this case we don't use a mask just calculate torsion with the entire iris, nothing
            // masked (works ok for many occasions)
            mask = new Image<Gray, byte>(rawImageEye.Size);
            var torsionAngle2 = torsionMethod.CalculateTorsionAngle(
                rawImageEye,
                this.calibration.EyePhysicalModel,
                this.calibration.ImageTorsionReference,
                mask,
                rawImageEye.EyeData.Pupil,
                rawImageEye.EyeData.Iris,
                this.settings,
                out imageTorsion,
                out dataQuality
                );

            
            //======================================================================================
            // Update UI 
            //======================================================================================
            
            // Update frame counter in UI
            toolStripTextBox1.Text = $"{this.video.LastFrameNumber}/{this.video.NumberOfFrames} frames";

            var imageForDisplay = ImageEyeDrawing.DrawAllData(rawImageEye, this.calibration, this.settings);

            this.imageBoxEye.Image = imageForDisplay;
        }

        private void restartToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            this.video?.Scroll(1);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.video?.Dispose();
            this.timer?.Dispose();
        }
    }
}
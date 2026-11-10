# Calibration pipelines

A calibration pipeline runs when the user presses **Calibrate** (or **Reset reference**). It turns
the tracked frames into the calibration that OpenIris uses from then on (`CalibrationParameters`,
saved in the `.cal` file).

A calibration has two stages, run one after the other on the frames as they are processed:

1. **The model** (`ProcessForCalibrationModel`): the eye models (each eye's center and size in its
   image), needed to convert pupil positions into rotation angles and to measure torsion; and, if
   the calibration has them, its own parameters and the head model. It may also set the references,
   and then the second stage is skipped.
2. **The zero reference** (`ProcessForZeroReference`): where the eyes and the head are when the
   subject looks straight ahead, and the iris images torsion is measured against.

**Reset reference** runs only the second stage, with the current models.

From the models and the references, OpenIris calculates the calibrated data
(`CalibrationParameters.GetCalibratedEyeData`). It must follow the
[coordinate system](../user-manual/data-file.md#coordinate-system) of the data file: positive is
rightward, upward and clockwise from the subject's point of view, and 0 is the reference. The
geometry (images, eye model, head model, frames) is described in
[Geometry and conventions](geometry.md).

See [Creating a plugin](creating-a-plugin.md) first for the project setup.

## The calibration class

Each stage is called with every new processed frame until it says it's completed. A frame
(`EyeTrackerImagesAndData`) has both eye images with their tracking data (`frame.Images[eye].EyeData`)
and the head data (`frame.Data.HeadDataRaw`: a head sensor, or the face pose of the Webcam system).
The simplest built-in calibration, `Auto` (`OpenIrisLib\Calibration\CalibrationPipelineAuto.cs`),
finishes each stage on the first frame where every eye is tracked:

```csharp
[Export(typeof(CalibrationPipelineBase)), PluginDescription("Auto", typeof(CalibrationSettings))]
public class CalibrationPipelineAuto : CalibrationPipelineBase
{
    public override (bool completed, CalibrationParameters? calibration) ProcessForCalibrationModel(EyeTrackerImagesAndData frame, EyeTrackingPipelineSettings processingSettings)
    {
        if (!AllEyesTracked(frame)) return (false, null);

        var calibration = new CalibrationParameters();
        foreach (var eye in Eyes(EyesToCalibrate))
        {
            var eyeData = frame.Images[eye]!.EyeData!;
            calibration.EyeCalibrationParameters[eye].SetEyeModel(new EyePhysicalModel(eyeData.Pupil.Center, (float)(eyeData.Iris.Radius * 2.0)));
        }

        return (true, calibration);
    }

    public override (bool completed, CalibrationParameters? calibration) ProcessForZeroReference(EyeTrackerImagesAndData frame, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
    {
        if (!AllEyesTracked(frame)) return (false, null);

        return (true, WithReferences(currentCalibration, frame));
    }
}
```

A real calibration would collect data over many frames (for example while the subject looks at
targets) before returning `true`. Helpers in the base class:

| Member | What it is |
| --- | --- |
| `EyesToCalibrate` | The eyes of the system: `Left`, `Right` or `Both`. A one-eye system has `null` images for the other eye. |
| `Eyes(EyesToCalibrate)` | Those eyes, one by one. |
| `AllEyesTracked(frame)` | Whether every eye of the system is tracked well in the frame. |
| `WithReferences(calibration, frame)` | A copy of the calibration with the references of every eye and of the head taken from one frame. |

## The contract

Everything not listed here is up to the calibration: what it collects, how it fits, what its
messages mean.

**OpenIris guarantees:**

- **One thread.** The two stages and `HandleMessage` are called from the calibration thread only,
  never at the same time, so a calibration needs no locks. Messages from clients are queued and
  delivered between frames, in the order they arrived.
- **Frames** in order (some may be skipped), with the raw tracking data of each eye and the head data
  of that frame.
- **The order:** "Calibrate" runs the model stage until it's completed, then the zero reference stage
  (unless the model already set the references). "Reset reference" runs only the zero reference
  stage. Cancelling keeps the previous calibration.
- **Fresh frames for the reference:** in the zero reference stage, only frames processed with the new
  eye models (torsion needs them). If the model stage didn't give an eye model for an eye, OpenIris
  makes one from the first of those frames (centered on the pupil, with twice the iris radius).

**The calibration must:**

- **Model stage:** when completed, return a calibration with an eye model for each eye in
  `EyesToCalibrate`, and optionally its own parameters (below), the head model, and the references.
- **Zero reference stage:** when completed, return the calibration with the references of each eye
  in `EyesToCalibrate` and of the head. They may come from one frame or from many (a median over a
  fixation, for example), but the eyes' and the head's must describe the same moment or period.
  OpenIris checks there's one for each eye.
- **Use the raw data** of the frames (`EyeData`, `HeadDataRaw`): their calibrated data comes from the
  calibration being replaced.
- **Be serializable:** everything it returns is saved in the `.cal` file.

## Optional methods

| Method | Use it to |
| --- | --- |
| `HandleMessage(message)` | Receive messages from a stimulus program during calibration: plain text sent with the web API, the `CALIBRATION\|message` remote command, or `CalibrationMessage` in MATLAB (see [Remote control](../user-manual/remote-control.md#calibration)). The meaning is yours; return "ack" or an error text, which goes back to the client. |
| `GetCalibrationUI()` | Show a panel in a Calibration tab of the main window while calibrating, with instructions or progress. Return a `UserControl` implementing `ICalibrationUIControl`. Better: export a panel class instead (see below). |
| `Dispose()` | Release resources. |

## A panel in the Calibration tab

To show instructions or progress in a Calibration tab while calibrating, keep the state in the
calibration class (public properties), and export a panel class that shows it. That way the
calibration class doesn't depend on Windows Forms. The panel classes are in OpenIrisWinForms (its NuGet package, see [Creating a plugin](creating-a-plugin.md)):

```csharp
[Export(typeof(ICalibrationPanel))]
public sealed class ExampleCalibrationPanel : CalibrationPanel<CalibrationPipelineExample>
{
    protected override ICalibrationUIControl? Create(CalibrationPipelineExample pipeline) => new ExampleCalibrationControl(pipeline);
}
```

`ExampleCalibrationControl` is a `UserControl` implementing `ICalibrationUIControl`. OpenIris calls
its `UpdateUI()` regularly on the user interface thread, where it reads the state of the pipeline.
See `OpenIrisWinForms\Calibration\CalibrationRemoteLinearUI.cs`.

## Examples in the source code

- `CalibrationPipelineAuto.cs`: the minimal calibration above.
- `CalibrationPipelineManual.cs`: the user sets the eye models by hand, with a panel of controls
  (`OpenIrisWinForms\Calibration\CalibrationPipelineManualUI.cs`).
- `CalibrationRemoteLinear.cs`: a stimulus program shows targets and tells OpenIris where they are
  with messages; OpenIris fits a linear transformation from the eye signal to the target position.

## What a calibration contains

`CalibrationParameters`, saved in the `.cal` file:

- **For each eye** (`EyeCalibrationParameters[eye]`, an `EyeCalibration`):
  - **The eye model** (`EyePhysicalModel`): the center of the eyeball in the image and its radius, in
    pixels, and the depth of the center (`CenterDepth`, 0 if unknown). See
    [Geometry and conventions](geometry.md#the-eye-model).
  - **The reference** (`SetReference` takes a processed frame): the eye data where the subject looks
    straight ahead (`ReferenceData`), the torsion image (`ImageTorsionReference`), and the image of
    the frame itself (`ReferenceImage`), to see or reprocess later.
  - **The intrinsics of the camera** in the pixels of the images the model and the reference are
    in (`Intrinsics`), copied by OpenIris from the frames: the model and the reference follow the
    images when the window of the camera moves. See
    [Geometry and conventions](geometry.md#the-camera).
  - **The extrinsics of the camera and the lights** (`Extrinsics`, `Lights`), copied by OpenIris from
    the system settings: where the camera is and how it's turned from the eye, and where the lights
    are from the eye. The calibrated angles take the head's up from the extrinsics.
  - **The calibration's own data for this eye** (`CalibrationData`, below).
- **The head** (`HeadCalibrationParameters`): its reference (`ReferenceHeadData`: the head angles are
  relative to it) and the head model (`HeadModel`: where the eyes are in the head).
- **The calibration's own parameters for the whole calibration** (`CalibrationModelParameters`,
  below).
- **The tracking settings** at the time (`TrackingSettings`), filled by OpenIris.

`CalibrationParameters.GetCalibratedEyeData` turns the pupil position into angles with the eye
model: the horizontal angle of the pupil is `asin((pupilX - centerX) / radius)`, and the same for
vertical. The calibrated position is that angle minus the angle of the reference pupil, so it is 0
when looking straight ahead, with the sign changed to OpenIris's convention (positive to the
subject's right and up, see [The data file](../user-manual/data-file.md#coordinate-system)): the
images show the eye as someone facing the subject sees it, with y down.

- Without an eye model, it uses one centered on the current pupil, with twice the iris radius.
- Without a reference, the reference is the center of the image.
- Torsion comes from the tracking pipeline, which receives the `EyeCalibration` in `Process` and
  measures the iris pattern against `ImageTorsionReference`. Without a reference image, torsion is
  `NaN`.
- The pupil area in mm² takes the eye model's radius as 12 mm.

## Which calibration to choose

- **Auto** sets the eye model as centered on the pupil when the subject looks straight ahead, with
  twice the iris radius, and takes the first frame processed with it as the reference. Enough when
  each camera faces its eye roughly straight on.
- **Manual** lets the user set the eye model on the image, for cameras that see the eye at an angle.
- **N-point** (`CalibrationPipelineRemoteLinear`, called Remote Linear before 0.2) fits a linear
  transformation from the eye signal to the target positions (`LinearGazeMap`), which then gives the
  horizontal and vertical positions instead of the eye model (see below). With the signal source
  `Appearance` it fits instead a ridge regression from the embedding of a gaze network
  (`EyeData.Appearance`, filled by the Webcam iris pipeline) to the targets (`AppearanceGazeMap`). The targets come in
  messages (Remote mode), or the pipeline shows them itself (`NPointSequence`): its `Target` is what
  to draw now, which the local targets window (`NPointTargetsForm`) and the web client's display page
  (through `/ws/calibration`) draw. A pipeline can stop the calibration itself with `Cancelled`.

## The calibration's own data and parameters

A calibration can produce more than the models and the references:

- **For each eye:** set `EyeCalibrationParameters[eye].CalibrationData` in the calibration it returns,
  to an object of a class inheriting `EyeCalibrationData`. For each frame tracked well, OpenIris calls
  its `Adjust(eyeData, fromEyeModel)` with the calibrated data computed from the eye model and the
  reference: return it changed as your calibration needs, replacing the angles (as `LinearGazeMap`
  does), correcting them (a pupil-size correction, for example), or anything else.
- **For the whole calibration** (a screen, a fit on both eyes together): set
  `CalibrationModelParameters` to an object of a class inheriting `CalibrationModelParameters`.

Both are kept in the `.cal` file with their type, so they must be serializable as XML (public
properties, a constructor without parameters), and be in the same assembly as the calibration.

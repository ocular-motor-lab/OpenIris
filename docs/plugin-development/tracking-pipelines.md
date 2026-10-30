# Tracking pipelines

A tracking pipeline processes one image of one eye and returns the eye data: pupil, corneal
reflections, iris, eyelids, torsion. OpenIris comes with several (JOM, SimpleCentroid,
SimplePupilCRBlobs); a plugin can add new ones.

See [Creating a plugin](creating-a-plugin.md) first for the project setup.

## The pipeline class

Inherit from `EyeTrackingPipelineBase` and implement `Process`:

```csharp
using System.ComponentModel.Composition;
using Emgu.CV;
using Emgu.CV.Structure;
using OpenIris;

[Export(typeof(EyeTrackingPipelineBase)), PluginDescription("Example", typeof(EyeTrackingPipelineExampleSettings))]
public sealed class EyeTrackingPipelineExample : EyeTrackingPipelineBase
{
    public override (EyeData data, Image<Gray, byte>? imateTorsion) Process(ImageEye imageEye, EyeCalibration eyeCalibrationParameters)
    {
        var settings = (EyeTrackingPipelineExampleSettings)Settings;

        // ... find the pupil in imageEye.Image ...
        var pupil = new PupilData(center, size, angle);

        var data = new EyeData
        {
            WhichEye = imageEye.WhichEye,
            Timestamp = imageEye.TimeStamp,
            ImageSize = imageEye.Size,
            ProcessFrameResult = ProcessFrameResult.Good,
            Pupil = pupil,
        };

        // The second value is the iris image used for torsion. null if not measuring torsion.
        return (data, null);
    }
}
```

- `imageEye.Image` is the grayscale image of one eye (an EmguCV `Image<Gray, byte>`).
- `eyeCalibrationParameters` has the current eye model and reference, for pipelines that use them
  (for example to measure torsion against the reference image).
- `Settings` is the pipeline's settings object and `WhichEye` the eye this instance processes.
- If the eye can't be tracked in an image, return data with a `ProcessFrameResult` saying why (for
  example `MissingPupil`) instead of throwing an exception.

A complete small example is `OpenIrisLib\ImageProcessing\EyeTrackingPipelineSimpleCentroid.cs`
(about 100 lines). `EyeTrackingPipelineJOM.cs`, next to it, is the full pipeline with corneal
reflections, eyelids and torsion.

> [!IMPORTANT]
> **Processing runs on several threads at once.** OpenIris creates a separate pipeline object for
> each eye and each processing thread, and the threads take turns with the frames. So:
>
> - fields of your pipeline are only used by one thread at a time, but an object does not see every
>   frame: don't rely on the previous frame having passed through the same object;
> - `static` fields are shared by all threads: avoid them, or protect them;
> - the settings object is shared: read it, don't change it from `Process`.
>
> `Process` also runs for every frame, up to 500 times per second per eye: keep it fast.

## Settings

Inherit the settings class from `EyeTrackingPipelineSettings` (see
[Settings](creating-a-plugin.md#settings)). It already contains the settings all pipelines share,
such as the pixel size (`MmPerPix`) and the regions to ignore around the image
(`CroppingLeftEye`, `CroppingRightEye`). A pipeline can also reuse the settings class of an existing
one, as SimpleCentroid does with `EyeTrackingPipelinePupilCRSettings`.

## Optional methods

| Method | Use it to |
| --- | --- |
| `GetQuickSettingsList()` | Show sliders for some settings below the eye images in the main window, such as the pupil threshold. Return a list of (label, range, setting name). |
| `UpdatePipelineEyeImage(whichEye, dataAndImages)` | Choose the image shown in the main window for this eye, for example with the pupil and reflections drawn on it. |
| `Dispose()` | Release resources the pipeline holds. |

Quick-settings sliders, from SimpleCentroid:

```csharp
public override List<(string text, RangeDouble range, string settingName)>? GetQuickSettingsList()
{
    var settings = (EyeTrackingPipelinePupilCRSettings)Settings;
    var settingName = WhichEye == Eye.Left
        ? nameof(settings.DarkThresholdLeftEye)
        : nameof(settings.DarkThresholdRightEye);

    return new List<(string, RangeDouble, string)>
    {
        ("Pupil threshold", new RangeDouble(0, 255), settingName),
    };
}
```

## Debugging images

`EyeTrackerDebug.AddImage("name", imageEye.WhichEye, image)` shows intermediate images in the Debug
tab while the **Debug** setting is on (Configuration, General settings). This slows processing down,
so it is only active then.

## Testing

The automated tests check that the built-in pipelines keep producing the same results (see
[Tests](contributing.md#tests)). For a new pipeline, the same tools (`PipelineHarness`,
`SyntheticEye`) can process a synthetic eye whose true pupil position is known, or the sample videos.

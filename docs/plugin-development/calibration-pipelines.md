# Calibration pipelines

A calibration pipeline runs when the user presses **Calibrate** (or **Reset reference**). It turns
the tracked images into the calibration that OpenIris uses from then on.

A calibration has two steps, run one after the other on the frames as they are processed:

1. **Eye model** (`ProcessForEyeModel`): the physical model of the eye (the eye's center and size in
   the image), needed to convert pupil positions into rotation angles and to measure torsion.
2. **Reference** (`ProcessForReference`): the frame that defines the zero position: where the eye is
   when looking straight ahead, and the iris image torsion is measured against.

**Reset reference** runs only the second step.

From the eye model and the reference, OpenIris calculates the calibrated data
(`CalibrationParameters.GetCalibratedEyeData`). It must follow the
[coordinate system](../user-manual/data-file.md#coordinate-system) of the data file: positive is
rightward, upward and clockwise from the subject's point of view, and 0 is the reference. How the
angles combine into an orientation (Fick, Helmholtz...) is up to the calibration. Today all
calibrations share the same calculation; one that calculates the angles its own way must keep the
signs and the zero, and say in its documentation which angles it uses.

See [Creating a plugin](creating-a-plugin.md) first for the project setup.

## The calibration class

Each method is called with every new processed frame until it says it's done. The simplest built-in
calibration, `Auto` (`OpenIrisLib\Calibration\CalibrationPipelineAuto.cs`), finishes on the first
good frame:

```csharp
[Export(typeof(CalibrationPipelineBase)), PluginDescription("Auto", typeof(CalibrationSettings))]
public class CalibrationPipelineAuto : CalibrationPipelineBase
{
    public override (bool modelCalibrationCompleted, EyePhysicalModel model) ProcessForEyeModel(ImageEye imageEye, EyeTrackingPipelineSettings processingSettings)
    {
        if (imageEye?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, EyePhysicalModel.EmptyModel);

        return (true, new EyePhysicalModel(imageEye.EyeData.Pupil.Center, (float)(imageEye.EyeData.Iris.Radius * 2.0)));
    }

    public override (bool referenceCalibrationCompleted, ImageEye? referenceData) ProcessForReference(ImageEye image, CalibrationParameters currentCalibration, EyeTrackingPipelineSettings processingSettings)
    {
        if (image?.EyeData?.ProcessFrameResult != ProcessFrameResult.Good) return (false, null);

        return (true, image);
    }
}
```

`imageEye.EyeData` is the result of the tracking pipeline for that frame. A real calibration would
collect data over many frames (for example while the subject looks at targets) before returning
`true`.

## Optional methods

| Method | Use it to |
| --- | --- |
| `GetCalibrationUI()` | Show a panel in a Calibration tab of the main window while calibrating, with instructions or progress. Return a `UserControl` implementing `ICalibrationUIControl`. Better: export a panel class instead (see below). |
| `HandleRemoteMessage(message)` | Receive messages from a stimulus program during calibration, sent with the `CALIBRATION\|message` remote command (see [Remote control](../user-manual/remote-control.md#calibration)). Return the reply. |
| `Dispose()` | Release resources. |

## A panel in the Calibration tab

To show instructions or progress in a Calibration tab while calibrating, keep the state in the
calibration class (public properties), and export a panel class that shows it. That way the
calibration class doesn't depend on Windows Forms. The panel classes are in `OpenIrisWinForms.dll`:

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
- `CalibrationPipelineManual.cs`: the user sets the eye model by hand, with a panel of controls
  (`OpenIrisWinForms\Calibration\CalibrationPipelineManualUI.cs`).
- `CalibrationRemoteLinear.cs`: a stimulus program shows targets and tells OpenIris where they are;
  OpenIris fits a linear transformation from eye position to target position.

<!-- To write (#75): explain how the eye model and reference are used, and which calibration to choose
when. -->

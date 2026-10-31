# The data file

Every recording, and every processed video, saves its data in a text file named like the recording
folder, for example `Subject01-2026Oct30-143502.txt` (see
[The files of a recording](tracking-and-recording.md#the-files-of-a-recording)).

For an example, download the
[output of processing the sample videos](https://github.com/ocular-motor-lab/OpenIris/tree/main/sample-data)
with the JOM pipeline: 40 seconds of both eyes at 100 Hz.

## Reading the file

The file has one line per frame, with values separated by spaces. The first line has the name of
each column.

=== "MATLAB"

    ```matlab
    data = readtable('Subject01-2026Oct30-143502.txt', 'Delimiter', ' ');
    plot(data.LeftSeconds, data.LeftPupilX)
    ```

=== "Python"

    ```python
    import pandas as pd

    data = pd.read_csv("Subject01-2026Oct30-143502.txt", sep=" ")
    data.plot(x="LeftSeconds", y="LeftPupilX")
    ```

## What the values mean

- **Positions are in pixels**, in the image of each eye as OpenIris shows it, after correcting the
  camera orientation: x to the right, y down, with the origin at the top left corner of the image.
  With cameras that move their region of interest (the arrows in the Set up tab), positions are
  relative to that region, so **don't move it during a recording**.
- **The data isn't calibrated.** To convert positions to eye angles, use the calibration saved next to
  the data, in the `.cal` file (see [Calibration](calibration.md)).
- **Zeros mean "not found"**: a frame where the pupil wasn't found still has its line, with the frame
  number and time, but zeros for the pupil, the reflections, the eyelids and torsion. An eye that isn't
  tracked has zeros everywhere.
- **A missing line means a dropped frame.** The frame number jumps by more than one.

## Columns for each eye

These come twice, first for the left eye (columns starting with `Left`), then for the right eye
(`Right`). For example `LeftPupilX` and `RightPupilX`.

| Column | Contents |
| --- | --- |
| `FrameNumber` | Frame number, counted from when tracking started (from the start of the video when processing a video). It increases by one per frame the camera captured, so a jump means dropped frames. |
| `FrameNumberRaw` | The camera's own frame counter, with whatever value it had when tracking started. For a video, the frame number in the video. |
| `Seconds` | Time the frame was taken, in seconds, from the camera's clock. For a video, the time in the video. Use it, not the computer time, for timing: it is exact even when the computer is busy. |
| `PupilX`, `PupilY` | Center of the pupil, in pixels. |
| `PupilWidth`, `PupilHeight` | Length of the two axes of the ellipse fitted to the pupil, in pixels (the full axes, not the radius). |
| `PupilAngle` | Angle of the pupil ellipse, in degrees. |
| `IrisRadius` | The iris radius set in the tracking settings, in pixels. It is not measured. |
| `Torsion` | Torsion, in degrees, relative to the reference image taken at calibration. 0 if torsion is off in the settings. `NaN` if torsion is on but there is no reference yet, for example before calibrating. |
| `UpperEyelid`, `LowerEyelid` | Eyelid height: the **sum** of the y positions of four points on the eyelid, in pixels (divide by 4 for their average). 0 if eyelids weren't tracked. |
| `DataQuality` | Quality of the torsion measurement, 0 to 100: how well the iris pattern matches the reference. 100 if torsion is off, 0 if the pupil wasn't found or there is no reference. Other pipelines may use it differently. |
| `CR1X`, `CR1Y` … `CR5X`, `CR5Y` | Centers of up to five corneal reflections, in pixels, nearest to the pupil first. 0 for reflections not found. |

## Columns for both eyes

| Column | Contents |
| --- | --- |
| `AccelerometerX`, `AccelerometerY`, `AccelerometerZ` | Head sensor accelerometer, in the units of the sensor. Only with eye tracking systems that have a head sensor (the Micromedical systems); 0 otherwise. |
| `GyroX`, `GyroY`, `GyroZ` | Head sensor gyroscope, as above. |
| `MagnetometerX`, `MagnetometerY`, `MagnetometerZ` | Head sensor magnetometer, as above. |
| `Int0` … `Int7` | Whole numbers that depend on the eye tracking system. With the Spinnaker systems, `Int0` and `Int1` are the state of the input lines of the left and right camera at the end of the exposure of each frame, one bit per line. Use them to synchronize with other equipment (see [Hardware](hardware.md#synchronizing-with-other-hardware)). |
| `Double0` … `Double7` | Decimal numbers that depend on the eye tracking system. None of the systems included with OpenIris use them. |
| `DebugTimeGrabbedLeft`, `DebugTimeGrabbedRight` | When OpenIris received the frame of each eye, in seconds since OpenIris started. |
| `DebugTimeProcessed` | When OpenIris finished processing the frame, in seconds since OpenIris started. The difference with the time grabbed is the processing delay. These three are computer times, for checking delays only: use `Seconds` for timing. |

## The events file

The events sent during a recording (with the remote control, see [Remote control](remote-control.md))
are saved in the `…-events.txt` file, one line per event:

```
Time=2026-10-30-14:35:10.123 FrameNumber=1520  Message=TrialStart Data=...
```

`FrameNumber` is the `LeftFrameNumber` of the last frame received when the event arrived
(`RightFrameNumber` if only the right eye is tracked), so events can be matched to the lines of the
data file.

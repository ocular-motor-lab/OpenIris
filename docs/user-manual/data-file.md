# The data file

Every recording, and every processed video, saves its data in two text files named like the
recording folder (see [The files of a recording](tracking-and-recording.md#the-files-of-a-recording)):

- **The data file**, for example `Subject01-2026Oct30-143502.txt`: what the eye tracker measured
  in the images, in pixels.
- **The calibrated data file**, `Subject01-2026Oct30-143502-calibrated.txt`: the eye and head
  positions in degrees, calculated with the calibration (see [Calibrated data](#calibrated-data)).

For an example, download the
[output of processing the sample videos](https://github.com/ocular-motor-lab/OpenIris/tree/main/sample-data)
with the JOM pipeline: 40 seconds of both eyes at 100 Hz.

## Reading the file

Both files have one line per frame, with values separated by spaces. The first line has the name of
each column. They have the same frames, in the same order.

=== "MATLAB"

    ```matlab
    data = readtable('Subject01-2026Oct30-143502.txt', 'Delimiter', ' ');
    plot(data.LeftSeconds, data.LeftPupilX)

    calibrated = readtable('Subject01-2026Oct30-143502-calibrated.txt', 'Delimiter', ' ');
    plot(calibrated.Seconds, calibrated.LeftH)
    ```

=== "Python"

    ```python
    import pandas as pd

    data = pd.read_csv("Subject01-2026Oct30-143502.txt", sep=" ")
    data.plot(x="LeftSeconds", y="LeftPupilX")

    calibrated = pd.read_csv("Subject01-2026Oct30-143502-calibrated.txt", sep=" ")
    calibrated.plot(x="Seconds", y="LeftH")
    ```

## What the values mean

- **Positions are in pixels**, in the image of each eye as OpenIris shows it, after correcting the
  camera orientation: x to the right, y down, with the origin at the top left corner of the image.
  With cameras that move their region of interest (the arrows in the Set up tab), positions are
  relative to that region, so **don't move it during a recording**.
- **Raw and calibrated data.** The data file is raw, in pixels. The calibrated data file has the
  eye positions in degrees, calibrated with the calibration in use at the time (see
  [Calibrated data](#calibrated-data)). The calibration is also saved next to the data, in the
  `.cal` file (see [Calibration](calibration.md)), so the raw data can be calibrated again later.
- **Zeros mean "not found"** in the data file: a frame where the pupil wasn't found still has its
  line, with the frame number and time, but zeros for the pupil, the reflections, the eyelids and
  torsion. An eye that isn't tracked has zeros everywhere. In the calibrated data file, where 0 is
  a valid angle, "not found" is `NaN`.
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
| `Torsion` | Torsion, in degrees, relative to the reference image taken at calibration, positive clockwise from the subject's point of view (see [Coordinate system](#coordinate-system)). 0 if torsion is off in the settings. `NaN` if torsion is on but there is no reference yet, for example before calibrating. |
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

## Region of interest

Added in version 0.2, after all the columns above, so code that reads older files keeps working.
For each eye (`Left` first, then `Right`):

| Column | Contents |
| --- | --- |
| `RoiX`, `RoiY`, `RoiWidth`, `RoiHeight` | Where the eye image is in the full image of the camera, in the camera's pixels: X and Y of its top left corner, and its size. 0 if unknown. |

What that is depends on the system:

- **Cameras that read only a window of their sensor** (Spinnaker, FlyCapture): the window, as the
  camera reports it with each frame. It changes when the window moves to follow the eye (the
  arrows in the Set up tab), so add `RoiX` and `RoiY` to `PupilX` and `PupilY` to get positions on
  the sensor.
- **Systems that cut each eye out of a bigger image:** the part cut out. For the Webcam system,
  which cuts a rotated square around each eye and scales it, it's the smallest rectangle around
  that square, so it's approximate. 0 when the face isn't found.
- **Other cameras, Simulation and videos:** the whole frame. When processing a recorded video,
  that's the frame of the video, not the window of the camera that recorded it.

These positions are in the camera's own image, before OpenIris rotates or mirrors it (see the
camera orientation of the system); they match the eye image only for cameras that are upright.

## Calibrated data

The `…-calibrated.txt` file, since version 0.2. (The 0.2 alpha versions had these columns in the
data file instead, between `DebugTimeProcessed` and `LeftRoiX`, without the gaze and up vectors.)

### Coordinate system

The eyes and the head use the same convention: **positive is rightward, upward and clockwise, all
from the subject's point of view.**

| Value | Unit | Positive when | Zero |
| --- | --- | --- | --- |
| Eye `H` | degrees | the eye looks to the subject's right | the eye at the calibration's reference |
| Eye `V` | degrees | the eye looks up | the eye at the calibration's reference |
| Eye `T` | degrees | the eye rotates clockwise as the subject sees it: the top of the eye moves towards the subject's right (counterclockwise in the eye image) | the iris at the calibration's reference |
| `HeadYaw` | degrees | the head turns to the subject's right | the head upright, facing straight ahead |
| `HeadPitch` | degrees | the head tilts up (chin up) | as above |
| `HeadRoll` | degrees | the head rotates clockwise as the subject sees it (right ear down) | as above |
| `HeadX`, `HeadY`, `HeadZ` | mm | the head moves to the subject's right (X), up (Y), forward (Z) | depends on the system (see below) |

The head's axes, with the head upright and facing straight ahead: X points to the subject's right,
Y up, and Z forward, out of the face. The eyes' gaze and up vectors use the same axes.

The signs and the zeros are the same for every eye tracking system and calibration method. How
the three angles combine into an orientation (Fick, Helmholtz, rotation vectors...) is not fixed:
it is up to each calibration method, which says which one it uses. All three angle definitions
agree near the reference; they differ for large eye or head rotations. The gaze and up vectors
(see [Columns](#columns)) give the same orientation without that choice.

How the calibration methods included with OpenIris (Auto, Manual, N-point) calculate the eye
angles:

- **H, V and T are Fick angles** (horizontal about the head's vertical axis, then vertical, then
  torsion about the line of sight) of the rotation of the eye from the reference.
- **The line of sight** goes from the center of the eye globe (the calibration's eye model) through
  the center of the pupil, turned by the eye model's angle kappa if it has one.
- **The rotation** takes the line of sight at the reference to the current one following
  Listing's law, with Listing's plane perpendicular to the line of sight at the reference, and then
  turns the eye about the line of sight by the torsion measured from the iris pattern, compared
  with the reference iris. In Fick angles, a rotation that follows Listing's law has a small
  torsion at oblique positions (tan(T/2) = tan(H/2)·tan(V/2): about 0.9° at 10° in both H
  and V, 2° at 15°, 3.5° at 20°), so T is the measured torsion plus that.
- **Before 0.2,** H and V were each the arcsine of the pupil's displacement on the globe, calculated
  separately, and T was the measured torsion alone. They are the same on the horizontal and
  vertical axes, and differ at oblique positions.
- **The signs assume the eye images show the eyes as seen by someone facing the subject:** the
  subject's right is the left of the image, and up is up. Each eye tracking system turns its images
  that way with its camera orientation (for example **VideoOrientation** in Simulation). A camera
  that sees the eye through a mirror needs a mirrored orientation. The Spinnaker systems assume the
  cameras see the eyes directly.
- **Without a calibration,** H and V are relative to the center of the eye image, with an eye globe
  whose radius is the iris diameter, and T is `NaN` (there is no reference iris).

Two kinds of systems measure the head:

- **The Webcam system** estimates the head pose from the face it finds in each image (see
  [Webcam tracking](webcam-tracking.md#head-pose)). The angles are relative to the head at the
  reference (**Calibrate** or **Reset reference**), or to facing the camera before there is one.
  The position is from the camera: `HeadX` to the subject's right and `HeadY` up, from the
  camera's axis, and `HeadZ` the distance to the camera. All `NaN` in frames where the face isn't
  found. The angles combine in Fick order: yaw, then pitch, then roll.
- **The Micromedical systems** have a head sensor. Their calibrated head values haven't been
  checked against this convention yet: the signs may differ, and `HeadYaw` can't really be measured
  with an accelerometer. The sensor's raw accelerometer and gyroscope are in the data file.

With other systems, all head columns are 0.

### Columns

First the frame:

| Column | Contents |
| --- | --- |
| `FrameNumber` | Frame number: `LeftFrameNumber` of the data file (`RightFrameNumber` if only the right eye is tracked). |
| `Seconds` | Time the frame was taken, in seconds: `LeftSeconds` of the data file (`RightSeconds` if only the right eye is tracked). |

For each eye, `Left` first, then `Right` (`LeftH`, `LeftV`, ... `RightEyeOpening`):

| Column | Contents |
| --- | --- |
| `H`, `V` | Eye position in degrees, as above. `NaN` if the pupil wasn't found. |
| `T` | Torsion in degrees, as above: the Fick torsion, so the measured `Torsion` plus that of Listing's law at oblique positions. With torsion off in the settings, `Torsion` is 0 and `T` is only Listing's part. `NaN` when `Torsion` is (on, but no reference yet). |
| `GazeX`, `GazeY`, `GazeZ` | The line of sight, as a unit vector: X to the subject's right, Y up, Z forward, the head's axes at the reference. (0, 0, 1) at the reference. The same direction as `H` and `V`, without the choice of angles. `NaN` if the pupil wasn't found. |
| `UpX`, `UpY`, `UpZ` | Where the top of the eye points, as a unit vector with the same axes, perpendicular to the gaze: (0, 1, 0) at the reference. With the gaze it gives the whole orientation of the eye, torsion included. When `T` is `NaN`, the one of Listing's law. |
| `PupilArea` | Area of the pupil ellipse in mm² (π × `PupilWidth` × `PupilHeight` / 4, converted to mm with the eye tracking system's pixel scale, **PixPerMm**). Before 0.2 it took the eye globe as 24 mm across. |
| `EyeOpening` | How open the eyelids are, in percent of the opening at calibration. 0 without a calibration reference. |

For the head:

| Column | Contents |
| --- | --- |
| `HeadRoll`, `HeadPitch`, `HeadYaw` | Head orientation in degrees, as above. |
| `HeadX`, `HeadY`, `HeadZ` | Head position in mm: from the camera with the Webcam system (above); 0 with other systems. |
| `HeadForwardX`, `HeadForwardY`, `HeadForwardZ` | Where the face points, as a unit vector with the axes above: (0, 0, 1) when the angles are 0. From the three angles in Fick order, like the eyes' gaze. |
| `HeadUpX`, `HeadUpY`, `HeadUpZ` | Where the top of the head points, as a unit vector with the same axes: (0, 1, 0) when the angles are 0. |

The 0.2 alpha versions also had the head's angular velocities and accelerations. They can be
calculated from the angles, and the raw gyroscope and accelerometer of the systems that have a head
sensor are in the data file.

## The events file

The events sent during a recording (with the remote control, see [Remote control](remote-control.md))
are saved in the `…-events.txt` file, one line per event:

```
Time=2026-10-30-14:35:10.123 FrameNumber=1520  Message=TrialStart Data=...
```

`FrameNumber` is the `LeftFrameNumber` of the last frame received when the event arrived
(`RightFrameNumber` if only the right eye is tracked), so events can be matched to the lines of the
data file.

## The video frames file

With **RecordVideo** on, the `…-videoframes.txt` file says where each frame is in the raw videos
(`…-Left.avi`, `…-Right.avi`), one line per frame written to them:

```
FrameNumber LeftVideoFrame RightVideoFrame
1520 0 0
1521 1 1
1523 2 NaN
```

- `FrameNumber` is the `LeftFrameNumber` of the frame (`RightFrameNumber` if only the right eye is
  tracked), as in the events file, so each line matches a line of the data file.
- `LeftVideoFrame` and `RightVideoFrame` are the positions of the frame in each video, counting from
  0 (add 1 for MATLAB's `read`). `NaN` means the frame isn't in that video: that eye had no image
  for it, or its size changed during the recording.
- Frames of the data file that aren't listed aren't in the videos: OpenIris couldn't write them fast
  enough (the video **drops** in the status bar), or **DecimateVideoRatio** skipped them.

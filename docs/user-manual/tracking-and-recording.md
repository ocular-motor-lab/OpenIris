# Tracking and recording

![The main window while tracking](../images/user-manual/main-window.jpg)

## Starting

Select the eye tracking system and click **Start Tracking** (or **File > Start tracking**). The
**Set up** tab shows the image of each eye with what OpenIris tracks drawn on it: the pupil, the
corneal reflections, the iris and, if torsion is on, the iris pattern used to measure it.

**File > Stop** stops tracking.

## Setting up the cameras

In the **Set up** tab:

- **Camera position and exposure**: the arrows move the region of the camera image that OpenIris
  uses, for each eye, if the camera's field of view is larger than that region. **+** and **−**
  change the exposure. These are kept for the next session.
- **Center eyes**: moves the regions so that each pupil is in the center.
- The **sliders** under each eye adjust the most used thresholds of the tracking pipeline, for
  example the pupil threshold (how dark the pupil must be) and the reflection threshold (how bright
  the reflections must be). All the other settings are in the [configuration](configuration.md).

Adjust the thresholds until the pupil and reflections are tracked steadily, in every direction the
subject will look.

## Recording

1. Type the **Session name**. It becomes the name of the files.
2. Check the **Data folder**. Click it to open it, or **…** to change it.
3. [Calibrate](calibration.md) if your experiment needs it.
4. Click **Start recording** (or **Record > Start Recording**). Click it again to stop.

Recordings can also be started and stopped by your stimulus program, which can mark events in the
data too (see [Remote control](remote-control.md)).

### The files of a recording

Each recording creates a folder in the data folder, named with the session name and the date and
time, for example `Subject01-2026Oct30-143502`. Inside, with the same name:

| File | Contents |
| --- | --- |
| `….txt` | The data, one line per frame. |
| `…-Left.avi`, `…-Right.avi` | The raw video of each eye, if **RecordVideo** is on. |
| `…-events.txt` | The events sent during the recording (see [Remote control](remote-control.md)). |
| `….cal` | The calibration used. |
| `…-settings.xml` | All the settings used. |
| `…-log.log` | The log of the recording. |

### The data file

The data file is a text file with one line per frame and values separated by spaces. The first line
has the name of each column, so it can be read directly, for example with `readtable` in MATLAB or
`pandas.read_csv(file, sep=" ")` in Python.

Positions are raw positions in the image, in pixels. The calibration needed to convert them to eye
angles is saved next to the data, in the `.cal` file.

For each eye (columns starting with `Left` and `Right`):

| Columns | Contents |
| --- | --- |
| `FrameNumber` | Frame number. It increases by one per frame captured, so gaps mean dropped frames. |
| `FrameNumberRaw` | The camera's own frame counter. |
| `Seconds` | Time the image was taken, in seconds (from the camera clock when the camera has one). |
| `PupilX`, `PupilY` | Pupil center. |
| `PupilWidth`, `PupilHeight`, `PupilAngle` | Size of the pupil ellipse, and its angle in degrees. |
| `IrisRadius` | Iris radius. |
| `Torsion` | Torsion angle, in degrees, relative to the reference. |
| `UpperEyelid`, `LowerEyelid` | Eyelid positions (when eyelid tracking is on). |
| `DataQuality` | Quality of the tracking of this frame, as estimated by the pipeline. |
| `CR1X`, `CR1Y` … `CR5X`, `CR5Y` | Centers of up to five corneal reflections. Zero when not found. |

Then, for all frames:

| Columns | Contents |
| --- | --- |
| `AccelerometerX` … `MagnetometerZ` | Head sensor data, for systems with a head sensor. |
| `Int0` … `Int7`, `Double0` … `Double7` | Extra values that depend on the eye tracking system. With the Spinnaker systems, `Int0` and `Int1` are the camera input lines (see [Hardware](hardware.md#synchronizing-with-other-hardware)). |
| `DebugTimeGrabbedLeft`, `DebugTimeGrabbedRight`, `DebugTimeProcessed` | When OpenIris received and finished processing the frame, in seconds since OpenIris started. Useful to check delays. |

Values of an eye that isn't tracked are zero.

## Watching for dropped frames

The status bar at the bottom shows the frame rate, the frames dropped and how many frames are
waiting to be processed. During a recording, watch for **red or yellow bars** that may appear in the
bottom left and right corners of the window: they mean OpenIris is falling behind. Lower the frame rate, turn off torsion or
**Debug**, or reduce the processing in the pipeline settings.

## The other tabs

- **Viewer**: the eye position traces over time, and the head data if there is a head sensor.
- **Log**: messages from OpenIris. Check it first when something doesn't work.
- **Debug**: intermediate images of the processing, when the **Debug** setting is on.
- **Timing**: how long each step takes.
- **Calibration**: appears while calibrating, if the calibration method has its own panel.

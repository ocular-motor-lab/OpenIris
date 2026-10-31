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
| `….txt` | The data, one line per frame (see [The data file](data-file.md)). |
| `…-Left.avi`, `…-Right.avi` | The raw video of each eye, if **RecordVideo** is on. |
| `…-events.txt` | The events sent during the recording (see [Remote control](remote-control.md)). |
| `….cal` | The calibration used. |
| `…-settings.xml` | All the settings used. |
| `…-log.log` | The log of the recording. |

### The data file

The data file is a text file with one line per frame and one column per value: the pupil position,
size and torsion of each eye, the corneal reflections, the eyelids, timestamps and more.
[The data file](data-file.md) describes every column and how to read it in MATLAB or Python.

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

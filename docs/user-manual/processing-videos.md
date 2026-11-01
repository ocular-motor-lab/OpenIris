# Playing and processing videos

Recordings made with **RecordVideo** on include the raw video of each eye. OpenIris can play them
back with tracking, to look at them, or process them again, for example with different settings or
a better calibration.

## Play video

**File > Play video…** (or **Play Video** on the main window) tracks a video as if it came from the
cameras: you can watch it, adjust the settings, calibrate and record, as with a live session.

## Process video

**File > Process video…** (or **Process Video**) tracks the whole video, or part of it, and saves the
data, frame by frame, without dropping any frame.

In the dialog:

| Option | |
| --- | --- |
| **Left eye video file**, **Right eye video file** | The videos to process. For a recording of both eyes, select the `-Left.avi` and `-Right.avi` files. |
| **Eye tracking system** | The system the videos were recorded with. Some systems read extra information stored in their videos, such as the camera timestamps. |
| **Calibration file** | Optional: a calibration to use, for example the `.cal` file of the recording. |
| **Play full video** / **Play only frames from … to …** | Process the whole video, or only part of it. |
| **Save processed video** | Also save a video of the processed images, with the pupil and reflections drawn on them. With both eyes, they are side by side. |

The tracking uses the current settings of the pipeline, so adjust them first (for example with
**Play video**) if needed.

The results go to the data folder, in a new folder named like the original recording followed by
`-PostProc` and the date and time, with the same kinds of files as a [recording](tracking-and-recording.md#the-files-of-a-recording),
plus the processed video if selected.

Processing doesn't drop frames, so it can run slower or faster than the original frame rate,
depending on the computer and the settings.

## Batch analysis

**Tools > Batch analysis** processes several videos in a row.

<!-- To write (#75): describe batch analysis. -->

## Other tools

The **Tools** menu also has tools to convert videos (to MP4 or RGB) and to trim them.

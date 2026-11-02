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

## From the command line

`OpenIrisProcess.exe`, next to `OpenIris.exe`, does what **Process video** does, from a command
line or a script: to process many recordings, or the same ones with other settings. It uses the
settings profiles of OpenIris, and OpenIris can be open at the same time.

```
OpenIrisProcess --profile "Spinnaker Dual Camera torsion" --calibration D:\Data\S01\S01.cal D:\Data\S01\S01-Left.avi D:\Data\S01\S01-Right.avi
```

| Option | |
| --- | --- |
| `VIDEO [VIDEO]` | One video with both eyes, or the left and right videos (`NAME-Left.avi` and `NAME-Right.avi`, or left first). |
| `--profile NAME` | The [settings profile](configuration.md#settings-profiles) with the pipeline and its settings. Default: the one used last. |
| `--system NAME` | The eye tracking system the videos were recorded with. Default: the profile's. |
| `--calibration FILE` | Optional: a calibration (`.cal`) for the calibrated data. |
| `--output FOLDER` | Where to save the data. Default: the data folder. |
| `--range FIRST-LAST` | Only these video frames, counting from 0. |
| `--save-video` | Also save the processed video. |
| `--verbose` | Show the log while processing. |

It prints the progress and, at the end, the data file. The results are the same as with **Process
video**. The exit code is 0 if the recording was processed, 1 for wrong arguments, and 2 if
processing failed, so scripts can tell which recordings failed. The settings aren't changed.

For example, to process every recording in a folder, from MATLAB:

```matlab
exe = 'C:\OpenIris\OpenIrisProcess.exe';
files = dir('D:\Data\*\*-Left.avi');
for i = 1:numel(files)
    left = fullfile(files(i).folder, files(i).name);
    right = strrep(left, '-Left.avi', '-Right.avi');
    status = system(sprintf('"%s" --profile "Spinnaker Dual Camera" "%s" "%s"', exe, left, right));
    if status ~= 0, fprintf('Failed: %s\n', left); end
end
```

Or from PowerShell:

```powershell
foreach ($left in Get-ChildItem D:\Data\*\*-Left.avi) {
    C:\OpenIris\OpenIrisProcess.exe --profile "Spinnaker Dual Camera" $left.FullName ($left.FullName -replace '-Left', '-Right')
    if ($LASTEXITCODE -ne 0) { "Failed: $left" }
}
```

## Batch analysis

**Tools > Batch analysis** processes several recordings in a row, in OpenIris, so you can watch
each one being processed (on the **Set up** tab). For batches run from a script, without watching,
see [From the command line](#from-the-command-line).

1. **Add session** selects a recording, with the same options as **Process video**. Repeat for each
   recording; **Remove session** takes the selected ones out of the list.
2. **Start** processes them one after the other. The **Progress** column says **Processing**,
   **Completed**, or **Failed** with the reason; a recording that fails doesn't stop the others.

The tracking uses the settings of the active profile, as with **Process video**. A recording
without a calibration file is processed without calibration (not with the previous recording's).

**Save batch description** saves the list in a text file, to process the same recordings again
later with **Load batch description**. The file has the folder of the recordings on the first line,
the eye tracking system on the second, the calibration file (in that folder) on the third, blank
for none, and then
one recording per line, by name, without `-Left.avi` or `-Right.avi`:

```
D:\Data\Experiment1
Spinnaker Dual Camera
S01.cal
S01-2026Nov01-101500
S02-2026Nov01-113000
```

Recordings loaded this way use the same calibration file and system, and don't save the processed
video.

## Other tools

The **Tools** menu also has tools to convert videos (to MP4 or RGB) and to trim them.

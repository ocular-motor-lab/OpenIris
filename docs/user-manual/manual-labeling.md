# Labeling videos by hand

**Tools > Label videos by hand** opens a window to label recorded videos frame by frame: click on
the eye images to mark the pupil, a reflection, or anything else. It replaces the old separate
SubjectiveTracker program. It only reads the videos: OpenIris doesn't need to be tracking, and
nothing is recorded.

## Labeling

1. **Tools > Label videos by hand**, and choose the videos: left and right, or only one.
2. The window shows the first frame of each eye, and the list of markers. Type a name in the empty
   row to add a marker (for example `Pupil`, `CR`, `Tip`). The markers are remembered for next time.

| To | Do |
| --- | --- |
| Place a marker | Select it in the list (or press 1 to 9), then click on the eye. It replaces the same marker on that eye and frame. |
| Remove a marker | Right click near it. **Delete** (or **Clear frame**) removes all the markers of the frame. |
| Move through the frames | Left and right arrows, or the buttons. **Step** sets how many frames each step moves (2 labels every other frame). **Step after each click** moves on after placing a marker. |
| Go back to labeled frames | **◀ labeled** and **labeled ▶**. |
| Play or pause | Space. |

Use AVI videos (OpenIris records AVI). Moving to a given frame of an MP4 video is not exact with the
video library OpenIris uses now, so labels could end up on the wrong frame.

## The labels file

The labels are saved as you go, next to the left video (or the only one), in `VIDEO-labels.csv`.
Opening the same videos again loads them, to continue or correct. The file has one line per marker:

| Column | Meaning |
| --- | --- |
| `Frame` | Frame number in the video, from 0. The same as `LeftFrameNumber` and `RightFrameNumber` in the [data file](data-file.md) of the processed video. |
| `Eye` | `Left`, `Right`, or `Both` for a single video with both eyes. |
| `Marker` | Name of the marker. |
| `X`, `Y` | Position in pixels of the eye image, with (0, 0) the center of the top left pixel. |

To use the labels with the data of a processed video, match them by frame number:

=== "MATLAB"

    ```matlab
    labels = readtable('Subject-Left-labels.csv');
    pupil = labels(strcmp(labels.Marker, 'Pupil') & strcmp(labels.Eye, 'Left'), :);
    % data: the data file, read as in "The data file"
    [~, rows] = ismember(pupil.Frame, data.LeftFrameNumber);
    errorX = data.LeftPupilX(rows(rows > 0)) - pupil.X(rows > 0);
    ```

=== "Python"

    ```python
    import pandas as pd
    labels = pd.read_csv("Subject-Left-labels.csv")
    pupil = labels[(labels.Marker == "Pupil") & (labels.Eye == "Left")]
    # data: the data file, read as in "The data file"
    merged = pupil.merge(data, left_on="Frame", right_on="LeftFrameNumber")
    error_x = merged.LeftPupilX - merged.X
    ```

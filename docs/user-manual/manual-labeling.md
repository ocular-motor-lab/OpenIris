# Labeling videos by hand

The **Manual Labeling** eye tracking system lets a person label recorded videos frame by frame: click on
the eye images to mark the pupil, a reflection, or anything else. The labels go into the data file
like tracking data, so they can be analyzed the same way, or compared with what a tracking pipeline
found. It replaces the old separate SubjectiveTracker program.

## Labeling

1. In **Configuration**, choose the tracking pipeline. **Nothing** gives data with only the labels;
   a real pipeline (for example **JOM**) tracks as usual and the labels are added to its data.
2. **Play video**, choose the left and right videos, and **Manual Labeling** as the eye tracking system.
3. The labeling window opens, paused at the first frame.

| To | Do |
| --- | --- |
| Place a marker | Select it in the list (or press 1 to 9), then click on the eye. It replaces the same marker on that eye and frame. |
| Remove a marker | Right click near it. **Delete** (or **Clear frame**) removes all the markers of the frame. |
| Move through the frames | Left and right arrows, or the buttons. **Step** sets how many frames each step moves (2 labels every other frame). **Step after each click** moves on after placing a marker. |
| Go back to labeled frames | **◀ labeled** and **labeled ▶**. |
| Play or pause | Space. |

The labels are saved as you go, next to the left video, in `VIDEO-labels.csv` (one line per marker:
`Frame,Eye,Marker,X,Y`, in pixels of the eye image). Playing the same video again loads them, to
continue or correct.

Use AVI videos (OpenIris records AVI). Moving to a given frame of an MP4 video is not exact with the
video library OpenIris uses now, so labels could end up on the wrong frame.

## Markers and the data file

The markers have free names. For each one, the list says where it goes in the data, separately for
each eye:

| Goes to | In the data file |
| --- | --- |
| None | Only in the labels file. |
| Pupil | `LeftPupilX`, `LeftPupilY` (or `Right...`), instead of what the pipeline found. The pupil size stays the pipeline's, or is NaN with **Nothing**. |
| CR1 … CR4 | `LeftCR1X`, `LeftCR1Y`... instead of what the pipeline found. |
| Double0and1 … Double6and7 | X and Y in two of the `Double` columns. |
| Int0and1 … Int6and7 | X and Y, rounded, in two of the `Int` columns. |

So, for example, a marker **Tip** that goes to Double0and1 for the left eye and Double2and3 for the
right eye gives `Double0` to `Double3` with its position on each eye. Frames without a marker keep
what the pipeline found (zero with **Nothing**). The calibrated columns are computed again from the
labeled pupil.

The markers are saved in the settings of the Manual Labeling system, so they stay the same from video to
video. They can be edited in the labeling window or in **Configuration**.

## Writing the data file

**Process video** with **Manual Labeling** as the system writes the data file with the labels of every
frame, as for any other system (see [Playing and processing videos](processing-videos.md)).

# Sample videos

`JorgeLeft.mp4` and `JorgeRight.mp4` are about 40 seconds of the left and right eye, recorded at
100 Hz with a mirrored camera. OpenIris includes them: the **Simulation** eye tracking system plays
them, and the tests use them.

The full quality recordings, `JorgeLeft.avi` and `JorgeRight.avi` (about 335 MB each), are too large
for git. If you have them, put both here (git ignores them) and Simulation plays them instead.

## Example output

`ProcessedWithJOM` has the files OpenIris writes when it processes the two MP4s
(**Process Video**) with the JOM tracking pipeline and the default settings:

| File | Contents |
| --- | --- |
| `….txt` | The data, one line per frame (3960 frames). |
| `….cal` | The calibration used (none: the default, uncalibrated). |
| `…-settings.xml` | All the settings used. Paths were changed to the default locations. |

The [user manual](https://omlab.berkeley.edu/OpenIris/user-manual/tracking-and-recording/#the-data-file)
describes the columns. The `DebugTime…` columns are processing times on the computer that made the
file.

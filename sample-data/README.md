# Sample data

The files OpenIris writes when it processes the [sample videos](../sample-videos) (**Process
Video**, or `OpenIrisProcess --system Simulation JorgeLeft.mp4 JorgeRight.mp4`) with the JOM
tracking pipeline and the default settings:

| File | Contents |
| --- | --- |
| `….txt` | The data, one line per frame: 3960 frames, 40 seconds of both eyes at 100 Hz. |
| `…-calibrated.txt` | The calibrated data, one line per frame. Without a calibration the angles are relative to the center of each eye image, and torsion is `NaN`. |
| `….cal` | The calibration used (none: the default, uncalibrated). |
| `…-settings.xml` | All the settings used. Paths were changed to the default locations. |

The log of the processing (`…-log.log`) is left out: it describes the computer that made the files.

The [user manual](https://omlab.berkeley.edu/OpenIris/user-manual/data-file/)
describes the columns. The `DebugTime…` columns are processing times on the computer that made the
file.

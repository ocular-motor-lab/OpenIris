# Sample videos

`JorgeLeft.mp4` and `JorgeRight.mp4` are about 40 seconds of the left and right eye, recorded at
100 Hz with a mirrored camera. OpenIris includes them: the build copies them to the `SampleVideos`
folder next to `OpenIris.exe`, where the **Simulation** eye tracking system plays them, and the tests
use them.

The full quality recordings, `JorgeLeft.avi` and `JorgeRight.avi` (about 335 MB each), are too large
for git. If you have them, put both here (git ignores them): the build copies them to `SampleVideos`
too, and Simulation plays them instead.

`Jorge-iPhone.MOV` is 20 seconds of the whole face, filmed with an iPhone (1920×1080, 30 fps). Play
it with the **Webcam** eye tracking system (Play video) to demo webcam tracking: the Webcam face
window shows the face mesh and the eye images cut out of it. It isn't copied next to `OpenIris.exe`;
open it from this folder.

[`sample-data`](../sample-data) has the output of processing these videos.

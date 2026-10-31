# Webcam tracking

OpenIris can track the eyes with an ordinary webcam that films the face. It is much less precise
than an infrared eye tracker, but it needs no special hardware, so it is useful for trying OpenIris,
for demonstrations, and for experiments where a few degrees of error are acceptable.

> [!WARNING]
> Webcam tracking is new and experimental. Its accuracy has not been measured yet.

## How it works

It uses neural networks from Google's [MediaPipe](https://github.com/google-ai-edge/mediapipe):

1. A face detector finds the face, only at the start or when the face was lost.
2. A face mesh finds 468 points on the face in every frame, including the corners of the eyes.
3. For each eye, OpenIris cuts an image out of the webcam image, centered between the eye corners,
   level, and always at the same scale. These eye images, shown in the main window, look like the
   images of a head-mounted eye tracker: the eye stays still in them when the head moves.
4. The iris network finds the iris and the eyelids in each eye image.

In visible light the pupil is hard to see, so the **iris** center stands in for the pupil center,
and the iris diameter for the pupil size, in the data file.

## Setting it up

1. Connect the webcam and place it below or above the screen, facing the subject, about 50–70 cm
   away. The face should fill a good part of the image: the more pixels on the eyes, the better.
2. Light the face evenly from the front. Avoid a bright window behind the subject.
3. In the [configuration](configuration.md), choose:
   - **Eye tracking system**: `Webcam`
   - **Tracking pipeline**: `Webcam iris`
4. Start tracking. The two eye images appear once the face is found, with the iris circle and the
   eyelids drawn on them.

A separate **Webcam face** window also opens, showing the whole webcam image with the face mesh
(green), the regions cut out as eye images (yellow), and the region searched for the face (blue).
Use it to check the framing and the lighting, and to see why tracking fails. It closes when tracking
stops; turn it off with `ShowFaceView`.

When the webcam starts, the log lists the webcams found and every format, resolution and frame
rate the one in use can send, followed by what it opened with. If the frame rate is lower than
expected, check there: many webcams give their high resolutions at full frame rate only in the
compressed MJPG format, which OpenIris asks for.

Settings of the Webcam system:

| Setting | Default | Meaning |
| --- | --- | --- |
| `CameraIndex` | 0 | Which webcam, if there are several (0 is the first). |
| `ResolutionWidth`, `ResolutionHeight` | 1280 × 720 | Resolution to ask the webcam for. Webcams choose the closest one they support. |
| `FrameRate` | 30 | Frame rate to ask for. Most webcams give 30 fps. |
| `EyeImageSize` | 128 | Size in pixels of each eye image. |
| `ShowFaceView` | on | Open the Webcam face window when tracking starts. |

## The data

Positions are in pixels of the eye images. The eye images are 2.3 times wider than the distance
between the eye corners, so with the default size of 128, one pixel is about 0.5 mm on the eye.
Because the eye images follow the head, head movements mostly don't change the iris position.

Recordings save the whole webcam image. They can be [processed again](processing-videos.md)
choosing the Webcam system.

## Limitations

- **Precision**: with 640×480 images, the eyes are only about 30 pixels wide, so expect noisy
  data. Higher resolutions help.
- **Frame rate**: about 30 fps. Finding the face takes ~15 ms per frame and each iris ~10 ms.
- **Timing**: webcams don't give timestamps or frame counters, so OpenIris uses the time each image
  arrives, and can't tell when the webcam itself dropped frames.
- **Blinks**: the networks always return an eye, so during a blink the iris position is not
  reliable. The eyelid positions show when the eye is closed.
- **Glasses** with strong reflections and faces turned far to the side make tracking worse.

The networks are licensed under the Apache License 2.0, a copy of which is in
`Plugins\WebcamModels\`. Where they come from and how they were converted is described in
`source\BasicPlugins\Webcam\Models\README.md`.

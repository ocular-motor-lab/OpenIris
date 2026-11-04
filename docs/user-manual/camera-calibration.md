# Camera calibration

**Tools > Camera calibration** measures each eye camera: how many pixels per mm it sees at the eye,
its focal length, where it is and how it's turned relative to the eye. These go in the system's
settings (**Camera properties**: `CameraIntrinsics`, `CameraExtrinsics`, `PixPerMm`), and the tool
writes a report with all of them.

Eye cameras see only 3 to 4 cm, with a few mm of depth of focus, so the usual OpenCV method (a
checkerboard tilted in many directions) doesn't work: a tilted board is mostly out of focus, and
with so narrow a view there is almost no perspective to measure. Instead, a flat dot grid sits where
the eye goes, facing forward, and moves in known steps along the head's forward axis, staying in
focus. How the dots grow and move between steps gives the distance and the angles of the camera.

## What you need

- **The target.** **Save printable target (PDF)** in the tool. The page has dot grids with 30 mm
  between the outer dots: A (1 mm pitch, the default), B (1.5 mm, for blurrier images) and C
  (0.75 mm). The missing dots at the center of each grid mark where the eye goes: the center, two to
  its right and one above it.
- **A laser printer and matte paper.** Print at 100% (*Actual size*, no fit to page). Inkjet ink can
  be invisible in infrared: check that the dots look black in the camera. Glossy paper reflects the
  LEDs.
- **Calipers**, to measure the printed grid: printers scale a little, and differently in x and y.
- **Something to hold the target where the eye goes**, upright, level, and facing forward: a plate on
  the chin rest or bite bar, or a 3D printed insert for goggles. Glue the grid flat on it.
- **To measure the distance and the angles: a way to move the target forward and back in known
  steps**, for example a small manual micrometer stage (10 to 13 mm travel, 0.01 mm marks) or
  spacers of known thickness. It must move along the head's forward axis: any tilt of the stage
  shows as tilt of the camera. Without it, the tool still measures the pixels per mm and the roll,
  and assumes the distance.

## Calibrating

1. Start tracking with the cameras, and open **Tools > Camera calibration**. Pick the eye.
2. Enter the measured distance between the outer dots, horizontally and vertically.
3. Put the target where the eye goes: printed side toward the camera, rows level, the center of the
   missing dots where the pupil would be. Enter how far behind the target the eye's center of
   rotation is: about 10 mm with the target where the pupil is.
4. Focus as for recordings. The tool shows the dots it finds in green and the center of the target
   as a red cross. The center must be in view. The window of the camera (its ROI) doesn't need to be
   bigger than for tracking.
5. With **Target position** at 0, **Capture**. The tool averages the frames (20 by default) to
   reduce noise.
6. Move the target forward (away from the face, toward the camera) by a known step, for example
   1.5 mm, enter the position, and capture. Then back, for example to -1.5 mm. Stay in focus.
7. **Calculate**. Check the report: the error of the fit should be a small fraction of a pixel.
8. **Apply to settings** sets the camera's intrinsics and extrinsics, and the system's `PixPerMm`
   (shared by both eyes). **Save report** saves the report as text. Restart tracking, and calibrate
   the eye again (**Calibration > Calibrate**).
9. Do the other eye.

## The report

| Value | Meaning |
| --- | --- |
| Error of the fit | How far the dots are from where the fitted camera puts them, root mean square, in pixels and in µm at the target. |
| Pixels per mm | At the target at position 0, where the pupil would be. |
| Focal length | In pixels. With all views at one position, from the assumed distance. |
| Distance camera to eye center | In mm. *ASSUMED* if all the views are at one position. |
| Camera seen from the eye | Degrees to the subject's left or right, and above or below straight ahead. |
| Head up in the image | The roll of the camera: how far the head's up is from the image's up. |
| Radial distortion k1 | OpenCV's k1. OpenIris doesn't use distortion yet. |
| Settings | The values for `CameraIntrinsics`, `CameraExtrinsics` and `PixPerMm`. |

The intrinsics are for the whole sensor, before the camera orientation, as the settings want them,
whatever window of the sensor the camera reads. The principal point stays at the center of the
sensor and the pixels are taken as square: with so narrow a view, neither can be measured. The
extrinsics follow [the geometry of OpenIris](../plugin-development/geometry.md).

## Problems

| Message | What to do |
| --- | --- |
| No target in view | More light, better focus, or grid B (bigger dots). |
| The center of the target is not in view or not clear | Move the target so the missing dots are in the image, and in focus. |
| The target looks mirrored | The camera orientation in the settings is wrong: the image must look as seen by someone facing the subject. |

The page can be regenerated with `python tools/make-calibration-target.py`.

# Camera calibration

**Tools > Camera calibration** measures the eye cameras: how many pixels per mm each one sees at the
eye, its focal length, where it is and how it's turned relative to the eye. These go in the system's
settings (**Camera properties**: `CameraIntrinsics`, `CameraExtrinsics`, `PixPerMm`), and the tool
writes a report with all of them. Both cameras are calibrated at once.

Eye cameras see only 3 to 4 cm, with a few mm of depth of focus, so the usual OpenCV method (a
checkerboard tilted in many directions) doesn't work: a tilted board is mostly out of focus, and
with so narrow a view there is almost no perspective to measure. Instead, a page of flat dot grids
sits where the eyes go, facing forward, and moves in known steps along the head's forward axis,
staying in focus. How the dots grow and move between steps gives the distance and the angles of
each camera.

## What you need

- **The target.** [Download it here](../downloads/OpenIris-calibration-targets.pdf), or **Save
  printable target (PDF)** in the tool. The page has three sizes of grid, each as a pair for the
  left and right eye, 63 mm apart (a typical adult eye distance), with a midline for the nose:
  A (1 mm pitch, the default), B (1.5 mm, bigger dots, for blurrier images) and C (0.75 mm). The
  missing dots at the center of each grid mark where the eye goes. How many there are tells the tool
  which grid and which eye it is, so it notices swapped cameras. The page faces the cameras, so the
  subject's right eye is on the left of the page.
- **A laser printer and matte paper.** Print at 100% (*Actual size*, no fit to page). Inkjet ink can
  be invisible in infrared: check that the dots look black in the camera. Glossy paper reflects the
  LEDs.
- **Calipers or a good ruler**, to measure the two 150 mm rulers of the print: printers scale a
  little (about 1 mm over 150 mm is common), and differently in x and y.
- **Something to hold the page where the eyes go**, upright, level, and facing forward: a plate on
  the chin rest or bite bar, or a 3D printed insert for goggles. Glue the page flat on it. For an
  eye distance other than 63 mm, cut the grids apart and glue each where its eye goes.
- **To measure the distance and the angles: a way to move the page forward and back in known
  steps**, for example a small manual micrometer stage (10 to 13 mm travel, 0.01 mm marks) or
  spacers of known thickness. It must move along the head's forward axis: any tilt of the stage
  shows as tilt of the cameras. Without it, the tool still measures the pixels per mm and the roll,
  and assumes the distance.

## Calibrating

1. Start tracking with the cameras, and open **Tools > Camera calibration**.
2. Enter what the horizontal and vertical 150 mm rulers of the print measure.
3. Put the page where the eyes go: facing the cameras, level, the midline on the nose, the center of
   each grid's missing dots where its pupil would be. Use one size (A, B or C) for both eyes. Enter
   how far behind the page the eyes' centers of rotation are: about 10 mm with the page where the
   pupils are.
4. Focus as for recordings. The tool shows both cameras, the right eye on the left. Found dots are
   green and the center of each grid is a red cross: it must be in view. Below the images it says,
   for each eye, which grid it sees and whether it can be captured. The window of the camera (its
   ROI) doesn't need to be bigger than for tracking.
5. With **Page position** at 0, **Capture**. The tool averages the frames of both cameras (20 by
   default) to reduce noise.
6. Move the page forward (away from the face, toward the cameras) by a known step, for example
   1.5 mm, enter the position, and capture. Then back, for example to -1.5 mm. Stay in focus.
7. **Calculate**. Check the report of each eye: the error of the fit should be a small fraction of
   a pixel.
8. **Apply to settings** sets the intrinsics and extrinsics of both cameras, and the system's
   `PixPerMm` (shared by both eyes: the mean of the two). **Save report** saves the report as text.
   Restart tracking, and calibrate the eyes again (**Calibration > Calibrate**).

## The report

One for each eye:

| Value | Meaning |
| --- | --- |
| Target | The grid, the eye, what the rulers measured, and the pitch of the print from them. |
| Error of the fit | How far the dots are from where the fitted camera puts them, root mean square, in pixels and in µm at the page. |
| Pixels per mm | At the page at position 0, where the pupil would be. |
| Focal length | In pixels. With all views at one position, from the assumed distance. |
| Distance camera to eye center | In mm. *ASSUMED* if all the views are at one position. |
| Camera seen from the eye | Degrees to the subject's left or right, and above or below straight ahead. |
| Head up in the image | The roll of the camera: how far the head's up is from the image's up. |
| Radial distortion k1 | OpenCV's k1. OpenIris doesn't use distortion yet. |
| Settings | The values for `CameraIntrinsics`, `CameraExtrinsics` and `PixPerMm`. |

The intrinsics are for the whole sensor, before the camera orientation, as the settings want them,
whatever window of the sensor the camera reads. The principal point stays at the center of the
sensor and the pixels are taken as square: with so narrow a view, neither can be measured. The
extrinsics of each camera are relative to the center of its eye, and follow
[the geometry of OpenIris](../plugin-development/geometry.md).

## Problems

| Message | What to do |
| --- | --- |
| No target in view | More light, better focus, or grid B (bigger dots). |
| The center of the grid is not in view or not clear | Move the page so the missing dots are in the image, and in focus. |
| The grid looks mirrored | The camera orientation in the settings is wrong: the image must look as seen by someone facing the subject. |
| It sees the grid of the other eye | The left and right cameras are swapped in the settings (or the page is upside down). |
| Use the same grid in all the views | All the views of an eye must be of the same size of grid. |

The page can be regenerated with `python tools/make-calibration-target.py docs/downloads/OpenIris-calibration-targets.pdf`.

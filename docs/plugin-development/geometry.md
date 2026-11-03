# Geometry and conventions

How OpenIris describes images, cameras, eyes and heads, in the terms computer vision uses. Where
a convention exists, OpenIris follows OpenCV's: OpenIris is built on it (through EmguCV), and
anyone who has used it already knows them.

This page describes what exists now and the direction agreed for the calibration redesign
([#61](https://github.com/ocular-motor-lab/OpenIris/issues/61)). Parts that don't exist yet are
marked **(planned)**. The conventions of the values in the data file, as users see them, are in
[The data file](../user-manual/data-file.md#coordinate-system).

## Units

- Lengths in **millimeters**, image positions in **pixels**.
- Angles in **radians** in the math, **degrees** in what users see (the data file, the user
  interface, the web API).
- Time in **seconds**.

## Images

**Orientation.** The images that reach the tracking pipelines have already been turned by the eye
tracking system's camera orientation (`CameraOrientation`: flips and 90° rotations), so that they
show the eye **as seen by someone facing the subject**: the subject's right is the left of the
image, and up is up. "The image" below always means this oriented image, not the raw sensor.

**Pixel coordinates**, as in OpenCV:

- x to the right, y down, origin at the top-left pixel.
- Integer coordinates are pixel **centers**: the top-left pixel is (0, 0), and its area goes from
  −0.5 to 0.5. Positions measured on resized images are mapped back with
  `ImageProcessingExtentionMethods.ResizedToOriginal`, which keeps this.
- `ImageEye.Roi` is where the image is in the camera's full sensor image (an offset, in the same
  pixel units), for cameras that read only part of the sensor.

## Ellipses

The pupil and the reflections are ellipses, as OpenCV's `RotatedRect`: center, full axes (width and
height), and an angle in degrees.

> [!WARNING]
> **`PupilData.Angle` is not the `RotatedRect` angle of the fit:** it is the angle
> `CvInvoke.FitEllipse` returns minus 90°, with the same width and height
> (`PositionTrackerEllipseFitting.GetPupilData`). To draw a pupil, do as `EyeImageOverlay` does:
> `new RotatedRect(pupil.Center, pupil.Size, pupil.Angle)`. New code should say which convention
> its ellipses use.

## The camera

**Today: weak perspective.** OpenIris treats the eye as flat at one depth, so a single scale links
pixels and millimeters: `MmPerPix`, the size of a pixel at the eye's depth (the eye tracking
system's setting, `EyeTrackingSystemSettings.MmPerPix`, copied into each pipeline's settings).
This is the usual model (scaled orthographic projection) when the camera-to-eye distance isn't
known. Its error grows at large eccentricities with cameras close to the eye.

**(planned) Intrinsics, as OpenCV's camera matrix**, settings of the eye tracking system, and
copied into the calibration parameters (the eye model is in pixels, so it needs the camera model it
was made with):

- The camera matrix K = [fx 0 cx; 0 fy cy; 0 0 1], in pixels, and distortion coefficients in
  OpenCV's order (k1, k2, p1, p2, k3). A camera calibrated with OpenCV's checkerboard calibration
  (`calibrateCamera`) can use its result directly; a tool to do it from OpenIris is
  [#78](https://github.com/ocular-motor-lab/OpenIris/issues/78).
- **Per physical camera**, not per eye: a list of camera calibrations in `EyeTrackingSystemSettings`,
  each with the camera's identity (its serial number, or Left, Right, Single for cameras without
  one), the sensor size it was measured at, and the intrinsics. It works for any number of cameras,
  and a camera's calibration follows it if the cameras are swapped. An empty list falls back to
  `MmPerPix` (below).
- **Measured on the full sensor image, without binning**, as OpenCV's calibration gives them.
  OpenIris converts them to the images it processes: the principal point shifts by the sensor
  window's offset (`ImageEye.Roi`), the camera orientation moves it and swaps fx and fy for 90°
  rotations (cx becomes width − 1 − cx when mirrored), and binning scales all four. So a camera
  calibration stays valid when the window or the orientation changes.
- **`MmPerPix` and the intrinsics don't conflict**: `MmPerPix` depends on the setup (the distance to
  the eye), the intrinsics only on the camera and lens. With both, the distance follows
  (Z = f · `MmPerPix`), a useful sanity check. There is no separate distance setting, which would
  have to agree with the other two.
- **Filled from `MmPerPix`** when nothing better is known: `MmPerPix` = Z / f, so one assumption,
  the camera-to-eye distance Z per kind of system (about 40 mm for head-mounted cameras, about
  600 mm for remote cameras and webcams), gives f = Z / `MmPerPix`; the principal point at the
  image center, square pixels, no distortion. Z only shapes the perspective correction: as Z grows,
  this becomes the weak perspective model above.
- **From the field of view** when the system knows it: f = (width / 2) / tan(FOV / 2). The Webcam
  system already does this for its head pose (`FacePose`, from `HorizontalFieldOfView`).
- The iris (about 11.5-12 mm across in almost everyone) is at the eye's depth, so it carries the
  same information as `MmPerPix`: a sanity check, not a way to find Z.

**(planned) The rig: where the cameras and the lights are.** The hardware is described in one
common frame, **the rig**: whatever the hardware is fixed to (the goggles or headset for head-mounted
systems, effectively the head; the camera mount for remote systems). In it:

- **Each camera's extrinsics:** its pose in the rig, `T_rig_camera`. The relative rotation of two
  cameras (they aren't parallel), needed for vergence, follows from their poses.
- **Each light** (the infrared LEDs that make the corneal reflections): its position in the rig, and
  which reflection it makes (CR0, CR1...). Their positions in any camera's frame follow from the
  camera's pose, so lights seen by both cameras are entered once. A physical model of the
  reflections needs them (with two or more lights, the cornea's center can be found in 3D, as in
  Guestrin and Eizenman's theory of remote gaze estimation, IEEE Transactions on Biomedical
  Engineering, 2006).

Like the intrinsics, the rig is a setting of the eye tracking system, copied into the calibration
parameters (a calibration may refine it), and every part is optional: without camera poses, each eye
is on its own (weak perspective, as today); without lights, the reflections are used empirically (pupil
minus reflection, Remote Linear's map), as today.

## Coordinate frames

| Frame | Axes | Units | Today |
|---|---|---|---|
| **Image** | x right, y down, origin top-left (oriented image, see above) | pixels | everything measured in the images |
| **Camera** | OpenCV's: x right, y down, z forward (out of the lens) | mm | the Webcam head pose (`FacePose`) |
| **Face** (Webcam) | x to the subject's left, y up, z out of the face; origin at the nose tip | mm | `FacePose`'s generic face model |
| **Output** (eye and head) | positive to the subject's right, up, and clockwise as the subject sees it | degrees, mm | the calibrated data ([data file](../user-manual/data-file.md#coordinate-system)) |
| **Head** | x forward, y to the subject's left, z up; origin at the midpoint between the centers of the eyes | mm | `HeadModel.NeckToEyes` |
| **Rig** (planned) | the frame the hardware is fixed to (the headset, or the camera mount) | mm | none yet |

**(planned) Transforms** are named by the frames they connect: `T_camera_head` is the 4×4 [R | t]
that takes points in head coordinates to camera coordinates (p_camera = T_camera_head · p_head).
Chains read right to left: `T_camera_head · T_head_eye` = `T_camera_eye`.

## Rotations

- **(planned) The head-fixed frame for eye rotations** is the one used in 3D oculomotor research
  (Haslwanter, "Mathematics of three-dimensional eye rotations", Vision Research, 1995):
  right-handed, x forward (the line of sight at the reference), y to the subject's left, z up, with
  rotations relative to the reference (primary) position. With the right-hand rule, positive
  rotations are leftward, downward, and clockwise as the subject sees it. OpenIris's output is
  positive **rightward, upward**, and clockwise, so the angle getters flip the signs of horizontal and
  vertical; torsion agrees.
- **In the math:** rotation matrices, quaternions, or rotation vectors (OpenCV's `Rodrigues`).
- **Euler-type angles** (Fick, Helmholtz) only as outputs, with the convention named. Today the
  three calibrated eye angles are each calculated separately (see the
  [data file](../user-manual/data-file.md#coordinate-system)); the Webcam head angles are in Fick
  order (yaw, then pitch, then roll).
- **(planned)** The calibrated eye data stores a rotation, with the angles as getters in one
  documented convention (the data file columns keep their names), and a "torsion known" flag,
  because horizontal and vertical are often known when torsion isn't. The calibrated head data
  stores a 4×4 pose.

## The eye model

`EyePhysicalModel` describes the eye globe **in the image**: `Center`, the center of the globe, and
`Radius`, its radius, both in pixels of the oriented image; `CenterDepth`, the distance of the center
from the camera along its axis, in the same pixel units (millimeters / `MmPerPix`), which with
`Center` places the center in 3D (0 if unknown, as in older calibration files: weak perspective);
`HorizontalVerticalRatio`, and cornea and kappa parameters. The calibrated angles and the torsion's geometric correction use it. A
calibration always ends with one: when a calibration pipeline doesn't provide it, the calibration
session makes one from the reference frame (centered on the pupil, with the iris diameter as
radius), or `EyePhysicalModel.GetDefault` (the image center, a 12 mm radius) if that frame has no
data. **(planned)** Every calibration pipeline provides it itself.

The cornea's front surface, which makes the first Purkinje image (P1, the corneal reflection), is
`CorneaRadius` and `CorneaPosition` (the distance from the globe's center to the cornea's center of
curvature), in pixels for backward compatibility. **(planned)** The lens's back surface, which makes
the fourth Purkinje image (P4, for dual-Purkinje tracking), in the same way: its radius of
curvature and the position of its center. Typical values (Gullstrand–Le Grand schematic eye, from
the center of rotation, about 13.5 mm behind the corneal apex): cornea radius 7.8 mm, center about
5.7 mm in front; lens back surface radius 6.0 mm, center about 11.9 mm in front. P4 is seen through
the cornea and the front of the lens, so a physical model of its position also needs the eye's
refractive indices (near-constant, from the same schematic eye).

**What a calibration measures beyond this model goes in its own parameters**: for each eye,
`EyeCalibration.CalibrationData` (an `EyeCalibrationData`), applied when it calculates the calibrated
data; for the whole calibration (a screen, a fit on both eyes together),
`CalibrationParameters.CalibrationModelParameters`. For example, per eye: Remote Linear's map from the
eye signal to the targets (`LinearGazeMap`), or, for example, a correction for the **pupil-size
artifact**: the pupil's center shifts (a few tenths of a millimeter) when the pupil changes size,
which looks like an eye movement of up to a degree or two, and like vergence with two eyes (see the
work of Hooge, Hessels, Nyström and colleagues, and Drewes, Masson and Montagnini).

**The head model** (`HeadCalibration.HeadModel`), parallel to the eye model, with the origin of the
head at the midpoint between the centers of the eyes:

- `LeftEye`, `RightEye`: the vector from that midpoint to the center of each eye, **in that eye's
  camera**: x, y in pixels, z (depth) in the same pixel units. Zero if unknown. No common coordinate
  system between the cameras is needed. With the head's pose in a camera (rotation R, the midpoint's
  position), the center of an eye in that camera is the midpoint + R · that eye's vector.
  Head-mounted cameras move with the head, so there the eyes don't move in the images when the head
  turns.
- `InterpupillaryDistance`: the distance between the centers of the eyes, in mm (63 by default).
- `NeckToEyes`: the vector from the point in the neck the head turns around to the midpoint between
  the eyes, in mm, in the head's frame, as the neck model of virtual reality headsets (80 mm forward
  and 75 mm up by default). It places the eyes when only the head's rotation is known.
- Still to add, when a head sensor can be checked
  ([#70](https://github.com/ocular-motor-lab/OpenIris/issues/70)): how the sensor is mounted on the
  head (a rotation from the sensor's axes to the head's).

## Gaze in 3D (planned)

3D eye trackers report, for each eye, a **gaze origin** (the eye's position, in mm) and a **gaze
direction** (a unit vector) in a stated frame; a gaze point (where the lines of sight meet, or
where they hit a screen) follows from them. A screen isn't part of the rig: a calibration that needs
one (where it is, its size) calculates it and keeps it in its `CalibrationModelParameters`. With the head model, the camera intrinsics and the eye
rotations, OpenIris has what's needed to report gaze the same way.

## Data quality

The terms used in eye tracking (Holmqvist and colleagues): **accuracy**, the mean offset from the
true position (a target, or the synthetic eye's truth); **precision**, the RMS of the differences
between consecutive samples (RMS-S2S) and the standard deviation; **data loss**, the share of
samples not tracked. `TrackingMeasurementTests` reports accuracy and precision this way (its
"frame-to-frame noise" is the RMS-S2S divided by √2, the noise of one sample), and the web client's
validation reports accuracy.

## Where things live

| What | Where |
|---|---|
| Camera orientation | the eye tracking system's settings (`CameraOrientation`), applied before tracking |
| `MmPerPix` | `EyeTrackingSystemSettings`, copied into each pipeline's settings |
| Pupil, reflections, torsion (raw) | `EyeData`, in image pixels and degrees |
| Eye model, reference | `EyeCalibration`, saved in the `.cal` file |
| Calibrated eye and head data | `CalibratedEyeData`, `CalibratedHeadData` |
| Head pose (Webcam) | `HeadData.FaceRotation`, `HeadData.FacePosition` |
| (planned) Intrinsics, camera poses, lights | the eye tracking system's settings, copied into the calibration parameters |
| Head model, head reference | `HeadCalibration`, saved in the `.cal` file |

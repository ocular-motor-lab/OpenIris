# Calibration

Calibration tells OpenIris the shape and position of each eye in the image (the eye model), and
where the eye is when looking straight ahead (the reference). OpenIris uses them to turn pupil
positions into eye rotation angles, and to measure torsion against the reference.

- **Calibrate** (on the **Set up** tab, or **Calibration > Calibrate**) runs the whole calibration.
- **Reset reference** only takes a new reference, keeping the eye model. Use it when the subject
  looks straight ahead again after a break, or after moving the cameras slightly. With systems that
  measure the head pose (Webcam), the reference also zeroes the head angles.
- **Calibration > Cancel calibration** stops a calibration in progress.
- **Calibration > Reset calibration** goes back to the default calibration.
- **Calibration > Save calibration…** and **Load calibration…** save and reuse a calibration. Each
  recording also saves the calibration used, in its `.cal` file.

## Calibration methods

Choose the method in the configuration (**CalibrationMethod**, Profiles tab, Eye tracking):

| Method | How it works |
| --- | --- |
| **Auto** | Takes the eye model and the reference from the first good frame. The subject must look straight ahead when you click Calibrate. |
| **Manual Calibration** | You set the eye model yourself, in a panel with controls for each eye. |
| **N-point** | The subject looks at targets at known positions; OpenIris fits a linear transformation from eye position to target position. OpenIris, a display it controls, or your stimulus program shows the targets: see [N-point](#n-point). Called **Remote Linear** before 0.2. |

Each calibration has two steps, one after the other: first the eye model of each eye, then the
reference, taken while the subject looks straight ahead.

### Which method to use

- **Auto**, the default, is enough for most recordings, and the quickest: useful with patients, when
  there is no time for a longer calibration. It assumes the eye's center of rotation is right behind
  the pupil when the subject looks straight ahead, with a radius of twice the iris radius. That
  works when each camera faces its eye roughly straight on.
- **Manual Calibration** is for when that assumption doesn't hold, for example when a camera sees
  the eye from the side or from below, or when you want to set the eye model precisely.
- **N-point** fits a linear transformation from the eye signal to the gaze angle, from
  targets at known positions (see [N-point](#n-point)). It is the most accurate when the subject
  can look at targets. The calibrated horizontal and vertical positions then come from that fit, and
  it is saved in the `.cal` file with the rest of the calibration. Choose the eye signal in its
  settings (**SignalSource** minus **SignalReference**):

  | Setup | SignalSource | SignalReference |
  | --- | --- | --- |
  | Pupil minus corneal reflection (the default; less sensitive to the camera moving on the head) | Pupil | CR1 |
  | Pupil alone, for example the webcam, which has no corneal reflections (the iris center) | Pupil | None |
  | Dual Purkinje (DPI): first corneal reflection minus the fourth Purkinje image | CR1 | the reflection with the 4th image, for example CR5 |
  | Webcam, from the appearance of both eyes instead of the iris (experimental, see [Webcam tracking](webcam-tracking.md#appearance-gaze)) | Appearance | ignored |

  The ready-made **WebcamMediaPipe** profile already uses the pupil alone, and **WebcamSaccadeJS** the
  appearance.

### N-point

Who shows the targets, and who decides where and when, is the calibration's **Mode**:

| Mode | The targets are shown by | Their positions and timing are decided by | Display |
| --- | --- | --- | --- |
| **Local** | OpenIris, full screen on a display of its computer | OpenIris | **StimulusDisplay** in the eye tracking system's settings: its number in Windows' display settings, 0 for the main display |
| **RemoteDisplay** | the web client, on the computer of the screen the subject looks at: any of its pages shows the targets while there are any | OpenIris | chosen in the web client (see [Remote control](remote-control.md#targets-shown-by-openiris)) |
| **Remote** (the default) | your stimulus program, or the web client's [full-screen calibration](remote-control.md#full-screen-calibration-and-validation) | that program, which tells OpenIris with messages | chosen in that program |

With **Local** and **RemoteDisplay**, press **Calibrate** in OpenIris (or the web client) and the
targets start: the center first, then the others in random order. Each target shrinks while the
eye settles on it (**SettleSeconds**), and then its data is collected (**CollectSeconds**). After
the last one, the subject looks at the center, which becomes the reference. Esc on OpenIris's
targets (Local) stops the calibration. The calibration's settings choose how many targets
(**Targets**: 5, 9 or 13), how far out they go (**TargetsHorizontal**, **TargetsVertical**, in
degrees from the center) and their size (**TargetSize**). The screen is part of the eye tracking
system's settings (section **Stimulus display**): its width (**ScreenWidthMm**) and the distance from
the eyes (**ViewingDistanceMm**), so the targets are at the right angles.

With **Remote**, the program sends the targets as they are shown: see
[Remote control](remote-control.md#calibration).

### Calibrating with Auto

1. Start tracking and check on the **Set up** tab that the pupil, the corneal reflections and the
   iris are tracked well in both eyes (see [Configuration](configuration.md)).
2. Ask the subject to look at a target straight ahead, at eye level, keeping the head still.
3. Click **Calibrate**. It finishes on the first frame where each eye is tracked well, so it takes
   a moment at most.

### Calibrating with Manual Calibration

1. Start tracking, and choose **Manual Calibration** as the calibration method.
2. Click **Calibrate**. A panel opens on the **Calibration** tab, with the image of each eye and
   sliders for the eye model: its horizontal and vertical position (**globe H**, **globe V**) and its
   radius (**globe R**), in pixels. The eye model is drawn as a circle on the images.
3. **Center globe** sets the sliders as Auto would, from the current pupil and iris. Adjust them so
   the circle matches the eyeball: centered on it, and as large as it.
4. Ask the subject to look straight ahead, and click **Set reference and accept**.

### Checking a calibration

- The circle of the eye model, on the eye images of the **Set up** tab, should be centered on the
  eyeball and about as large as it.
- While the subject looks straight ahead, the calibrated horizontal, vertical and torsional
  positions should be close to 0 (traces on the **Viewer** tab).
- Ask the subject to look at targets at known angles, for example 10° right, left, up and down.
  The calibrated positions should be close to them, with the [signs](data-file.md#coordinate-system)
  OpenIris uses: positive to the subject's right and up. They come from a model of the eye, so expect some error,
  larger at larger angles.
- Torsion should stay close to 0 while the subject looks straight ahead. If it drifts, or after a
  break or moving the cameras, use **Reset reference**.

Recordings save the calibration in their `.cal` file, so you can also [process](processing-videos.md)
them again later with a better one.

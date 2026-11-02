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

Choose the method in the configuration (**CalibrationMethod**, General settings, section C):

| Method | How it works |
| --- | --- |
| **Auto** | Takes the eye model and the reference from the first good frame. The subject must look straight ahead when you click Calibrate. |
| **Manual Calibration** | You set the eye model yourself, in a panel with controls for each eye. |
| **Remote Linear** | Your stimulus program shows targets and tells OpenIris where they are; OpenIris fits a linear transformation from eye position to target position. See [Remote control](remote-control.md#calibration). |

Each calibration has two steps, one after the other: first the eye model of each eye, then the
reference, taken while the subject looks straight ahead.

### Which method to use

- **Auto**, the default, is enough for most recordings, and the quickest: useful with patients, when
  there is no time for a longer calibration. It assumes the eye's center of rotation is right behind
  the pupil when the subject looks straight ahead, with a radius of twice the iris radius. That
  works when each camera faces its eye roughly straight on.
- **Manual Calibration** is for when that assumption doesn't hold, for example when a camera sees
  the eye from the side or from below, or when you want to set the eye model precisely.
- **Remote Linear** fits a transformation from eye position to target position, from targets shown
  by a stimulus program. The fit is saved, but not applied to the data yet (#61). For calibrated
  data, use Auto or Manual Calibration for now.

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

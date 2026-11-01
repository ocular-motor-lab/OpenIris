# Calibration

Calibration tells OpenIris the shape and position of each eye in the image (the eye model), and
where the eye is when looking straight ahead (the reference). OpenIris uses them to turn pupil
positions into eye rotation angles, and to measure torsion against the reference.

- **Calibrate** (on the **Set up** tab, or **Calibration > Calibrate**) runs the whole calibration.
- **Reset reference** only takes a new reference, keeping the eye model. Use it when the subject
  looks straight ahead again after a break, or after moving the cameras slightly.
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

<!-- To write (#75): when to use each method, step by step instructions, and how to check that a
calibration is good. -->

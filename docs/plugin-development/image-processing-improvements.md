# Image processing: known issues and planned improvements

Found in October 2026 while writing the pipeline tests (`source/OpenIrisTests`). Items marked **Fixed**
have been changed since. Fixing any of them **changes recorded data**, so each fix should:

1. be made on its own branch;
2. tighten the matching limit in `PipelineAccuracyTests` so the tests prove the improvement;
3. update the baselines (`OPENIRIS_UPDATE_BASELINES=1`; see `source/OpenIrisTests/README.md`), so the
   CSV diff shows exactly how much each value moved;
4. confirm with the speed test that processing is not slower (high frame rates depend on it).

Pixel numbers below assume the default scale of 6 pixels per mm.

## 1. Half-pixel offsets in JOM pupil and reflection positions (most important)

**Fixed** (#52): odd blur sizes and `(x + 0.5) / s - 0.5`. On the synthetic eye the mean error went
from 0.83 to 0.07 px for the pupil and from 1.06 to 0.06 px for the reflection; torsion and speed
didn't change. The JOM accuracy limits are now 0.3 px.

On the synthetic eye, JOM reports positions shifted right and down from the truth:

| | x offset | y offset |
|---|---|---|
| Pupil center | +0.60 px | +0.55 px |
| Corneal reflection center | +0.75 px | +0.75 px |

Two OpenCV conventions cause this, and the arithmetic matches the measurements:

- **Even-sized box blurs.** OpenCV can't center an even kernel, so each even `SmoothBlur` moves the
  image half a pixel right and down.
- **Converting back from a resized image.** OpenCV's `Resize` aligns pixel centers, so a point at `x`
  in an image scaled by `s` maps back to `(x + 0.5) / s - 0.5`. The code uses `x / s`, which adds
  `0.5 - 0.5 / s` pixels (+0.25 for s = 2, +0.33 for s = 3, -0.5 for s = 0.5).

### Reflection center: `CornealReflectionTracking.cs`

- Line 257: `SmoothBlur(smoothBlurSize, smoothBlurSize)` where `smoothBlurSize = blurSize + 1` = 2 by
  default (even): +0.5 px.
- Line 257 then line 276: `Resize` by `scaleFactor_findCenter = 2`, mapped back with
  `centerMass.X / scaleFactor_findCenter + crROI.X`: +0.25 px.
- Total: +0.75 px in each axis, which is what the tests measure. Changing the blur to an odd size
  moved the reflection about 0.5 px toward the truth.
- The same resize-mapping error is in the first, coarse stage (line 122, scale 0.5, mapped back at
  lines 211 and 227). It only chooses the region for the second stage.

### Pupil center: `PositionTrackerEllipseFitting.cs`

`GetBinaryPupilImage` enlarges the pupil region to 200 px (scale `s` ≈ 3 for a 22 px pupil), then:

- Line 595: `SmoothBlur(10, 10)`, always even: +0.5 px in the enlarged image, so `0.5 / s` ≈ +0.17 px.
- Line 594: `SmoothBlur(blurSize, blurSize)` with `blurSize = round(2 * s)`: even for some pupil sizes
  (another +0.17 px), odd for others (nothing).
- Line 687: mapped back with `pupilEllipse.Center.X / scaleFactorX + roiPupil.X`: `0.5 - 0.5 / s` ≈ +0.33 px.
- Total: about +0.5 to +0.67 px depending on pupil size.

The Centroid and ConvexHull position methods use the same `GetBinaryPupilImage`, so they probably
share the offset.

### Why it matters

- **It depends on pupil size.** The offset changes with `s`, so pupil dilation or constriction shifts
  the reported position even when the eye doesn't move.
- **It jumps.** When pupil size makes `round(2 * s)` flip between odd and even, the position jumps by
  about 0.17 px. That's about 0.03 mm, or roughly 0.1° of eye rotation (rough estimate), around the
  size of the smallest microsaccades.
- **It only partly cancels in pupil-minus-reflection measurements,** because the two offsets differ.
- A constant offset alone would cancel after calibration; these varying parts don't.

### Fix

- Use odd kernel sizes: 9 or 11 instead of 10; `2 * round(s) + 1` instead of `round(2 * s)`; 3 instead
  of 2 for the reflection blur. The amount of smoothing barely changes, and OpenCV's box blur costs
  the same for any size, so speed shouldn't change.
- Map points back from resized images with `(x + 0.5) / s - 0.5` (in both trackers, and anywhere else
  coordinates come back from a resized image).
- Then tighten the JOM accuracy limits in `PipelineAccuracyTests` from 1.5 px to about 0.2 px.

## 2. Reflection size limits use the wrong area formula

**Fixed** (#53). Torsion didn't change; a tiny tear-film highlight at the upper eyelid is now found
as a second reflection on some frames of the sample video.

`CornealReflectionTracking.cs`, lines 60-61:

```csharp
var maxBlobArea = trackingSettings.MaxCRRadPix * Math.PI * Math.PI;   // radius × π², not π × radius²
var minBlobArea = trackingSettings.MinCRRadPix * Math.PI * Math.PI;
```

`PupilTracking.cs` (line 62) computes the pupil's area correctly as `Math.PI * Math.Pow(radius, 2)`.

With the defaults (reflection radius 0.3 to 5 mm, so 1.8 to 30 px), the limits actually in effect are
about 2.4 to 9.7 px. Reflections smaller than about 2.4 px or larger than about 9.7 px (1.6 mm) are
rejected. This probably contributes to item 3.

Fix: `Math.PI * Math.Pow(radius, 2)`. Check on real recordings that larger limits don't let other
bright spots (for example reflections on glasses) through. The settings may need retuning.

## 3. Small reflections are missed between pixels

A 3 px-radius reflection on the synthetic eye was found in only 1 of 100 frames: the one whose center
fell exactly on a pixel. The first stage blurs, shrinks the image to half size and thresholds at 250,
which pulls a small reflection's peak below the threshold unless it's centered on a pixel. The
synthetic eye uses a 5 px reflection so the tests can run.

This matters for cameras at lower magnification, or for small or dim reflections. Options: skip the
half-size step when reflections are small in pixels (the code already has a comment about this), and
fix item 2.

## 4. SimplePupilCRBlobs pipeline doesn't work

- **Synthetic eye:** it never finds the pupil, and reports every frame as Good at (0, 0).
- **Sample video:** it reports a pupil about 15 px wide, somewhere other than where JOM finds a pupil
  about 61 px wide.
- **Likely causes:** it thresholds the pupil to white, then runs `SimpleBlobDetector`, which by
  default looks for dark blobs. It also never sets `ProcessFrameResult` to anything but Good.
- **Options:** fix it, or remove it from the pipeline list. It has baselines, so any fix will show up
  in the tests.

## 5. Smaller items

- **Torsion mask erosion** (`TorsionTracking.cs:404`): a 15×31 kernel with anchor (8, 16). The centered
  anchor is (7, 15), so the mask shifts by 1 px. It affects which pixels are used, not the angle
  directly.
- **Pupil mask erosion** (`PositionTrackerEllipseFitting.cs:399-402`): kernel size `width / 10` = 20,
  which is even, so the mask shifts by half a pixel. It affects which contour points are kept.
- **Eyelid tracking** (`EyeLidTracking.cs:413`): `SmoothBlur(4, 4)` is even. Eyelid tracking is off by
  default.
- **Reflection search region:** `distanceToPupil` (`CornealReflectionTracking.cs:191`) measures from the
  center of the search region, which is the pupil only if the region wasn't clipped at the image edge.
- **`VideoEye.GrabImageEye`** (`ImageGrabbing/VideoEye.cs:107-112`, not image processing): it uses
  `image.TimeStamp` before checking `image == null`, so a null frame throws instead of returning null.
- **CvBlob: done.** `Emgu.CV.Cvb` was replaced by `ImageProcessing/BlobDetector.cs` (plain C#, no
  OpenCV). It gives exactly the same blobs in the same order (checked by `BlobDetectorTests` on about
  1,800 images, and all pipeline baselines are unchanged) and is about 6 times faster (11 µs versus
  66 µs per call in Release). OpenCV's `ConnectedComponentsWithStats` was tried first: same blobs, but
  a different order and slower than CvBlob on these small images. OpenIris no longer needs
  `Emgu.CV.Cvb`, so EmguCV can now be upgraded. The OpenIrisDPI plugin only has an unused
  `using Emgu.CV.Cvb;` line, which it will need to remove.

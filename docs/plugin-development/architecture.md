# How OpenIris works

This page gives an overview of how images flow through OpenIris, which code runs on which thread, and
where plugins fit in. It helps to read it before writing a plugin or changing OpenIris itself.

## The main objects

`EyeTracker` (in `OpenIrisLib\EyeTracker.cs`) is the center of OpenIris. The main window, the remote
services and the tests all control OpenIris through it. While tracking, it owns:

| Object | Role |
| --- | --- |
| `EyeTrackingSystem` | The selected [eye tracking system](eye-tracking-systems.md) plugin. Creates the cameras. When playing a video, a `VideoPlayer` plays this role. |
| `ImageGrabber` | Reads images from the cameras (or videos) and pairs the images of both eyes. |
| `ImageProcessor` | Runs the [tracking pipeline](tracking-pipelines.md) on each frame, on several threads. |
| `HeadTracker` | Reads head movement data, for systems that have it. |
| `RecordingSession` | Saves data, events and videos to disk while recording. |
| `CalibrationSession` | Runs the [calibration](calibration-pipelines.md) while calibrating. |
| `Settings` | All settings, including each plugin's, saved in the active [settings profile](../user-manual/configuration.md#settings-profiles) (`Settings\NAME.xml`), except the ones marked `[SharedSetting]`, saved in `OpenIrisSettings.xml`. |

## The path of a frame

```
 cameras ──► ImageGrabber ──► ImageProcessor ──► EyeTracker ──► recording, calibration,
 (one thread    (pairs left       (pipelines on      (builds the      main window,
  per camera)    and right)        several threads)   frame's data)    remote clients
                    │
                    └──► RecordingSession (raw video)
```

1. **Grabbing.** Each camera runs `GrabImageFromCamera` in a loop on its own thread. The grabber
   pairs the left and right images by frame number, counts dropped frames, and raises
   `ImagesGrabbed`.
2. **Raw video.** If recording with video, the raw images go to the recording, which writes them on
   its own thread.
3. **Processing.** The frame goes into the processor's queue. Each processing thread takes a frame,
   calls the system's `PreProcessImages` (for example to split one image into two eyes), then the
   pipeline's `Process` for the left and right eye in parallel. Frames can finish out of order, so
   the processor puts them back in order before passing them on (`ImagesProcessed`).
4. **Data.** `EyeTracker` combines the eye data with head data and the calibration
   (`EyeTrackerData`: raw and calibrated eye positions), calls the system's
   `PostProcessImagesAndData`, and keeps the result as `LastImagesAndData`.
5. **Consumers.** The frame is then saved (data file, processed video), passed to the calibration in
   progress, and announced with `NewDataAndImagesAvailable` (used by remote clients). The main window
   doesn't follow every frame: a timer regularly shows the latest one, `LastImagesAndData`.

### Real time and offline

- With **cameras**, OpenIris works in real time: if processing falls behind and the queue fills up,
  frames are dropped (and counted) rather than delayed.
- With **videos** (Play video and Process video), nothing is dropped: grabbing waits for processing.
  This is why processing a video may run faster or slower than its original frame rate.

## Threads

| Thread | Runs |
| --- | --- |
| Main (user interface) | Windows, buttons, the timer that refreshes the images and traces. |
| One per camera | `CameraEye.GrabImageFromCamera`. |
| Processing (several) | `PreProcessImages`, the pipeline's `Process`. The number is set by **MaxNumberOfProcessingThreads**. |
| Recording (one per file type) | Writing data, events, raw and processed videos. |
| Remote services | Answering remote commands (TCP, UDP, .NET, web). |

For plugin code, this means:

- code in a camera's grab loop must be fast and must not wait for the user interface;
- pipeline objects are used by one processing thread at a time, but several objects run at once (see
  [Tracking pipelines](tracking-pipelines.md#the-pipeline-class));
- events such as `NewDataAndImagesAvailable` are raised on processing threads, not on the user
  interface thread.

## Plugins

At startup, `EyeTrackerPluginManager` loads the plugin classes from OpenIrisLib and from the DLLs in
the `Plugins` folder (see [Creating a plugin](creating-a-plugin.md#how-openiris-finds-plugins)). The
configuration window lists them, and `Settings` keeps the settings of each one, so switching between
systems or pipelines keeps their settings.

## The user interface

OpenIrisLib, which has `EyeTracker` and the base classes for plugins, doesn't use Windows Forms. The
main window and everything else on screen are in OpenIrisWinForms and OpenIris.exe. Plugins follow
the same split: a system or calibration class has no windows; it exposes its state, and separate view
classes show it ([a system's window](eye-tracking-systems.md#a-window-for-the-system),
[a calibration panel](calibration-pipelines.md#a-panel-in-the-calibration-tab)). The user interface
finds them with `EyeTrackerPluginManager.GetExports<T>()`. Another user interface, for example on
another operating system, would look for its own kind of views of the same plugins.

## Images and memory

Images are EmguCV `Image<Gray, byte>` objects. OpenIris doesn't dispose frame images explicitly; the
.NET garbage collector frees them when no longer used. Code that reads image pixels through a raw
pointer must keep the image object alive while doing so (`GC.KeepAlive`). To show an image in a
window, convert it with `ToBitmap()` (from the Emgu.CV.Bitmap package), which makes a copy.

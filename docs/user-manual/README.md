# OpenIris user manual

For people running eye tracking experiments with OpenIris. No programming needed. (To extend
OpenIris, see the [plugin development manual](../plugin-development/README.md).)

Something not working? Check [Troubleshooting](troubleshooting.md), and
[report bugs on GitHub](https://github.com/ocular-motor-lab/OpenIris/issues).

## Basics

What you need to run experiments.

1. [Installation](installation.md): requirements, download and set up, first run with the Simulation
   system.
2. [Configuration](configuration.md): the settings that matter and recommended values.
3. [Tracking and recording](tracking-and-recording.md): setting up the cameras, recording, the files
   of a recording.
4. [Calibration](calibration.md)
5. [The data file](data-file.md): every column of the data, and how to read it in MATLAB or
   Python.
6. [Remote control](remote-control.md): controlling OpenIris from MATLAB, Python or your stimulus
   program.
7. [Playing and processing videos](processing-videos.md)
8. [Webcam tracking](webcam-tracking.md): eye tracking with an ordinary webcam, no special hardware.
9. [Troubleshooting](troubleshooting.md)

## Advanced

For setting up a new system, special uses, and tools you need now and then.

- [Camera calibration](camera-calibration.md): measuring the eye cameras (pixels per mm, focal
  length, position and angles) with a printed dot grid, with the tool in the Tools menu.
- [Labeling videos by hand](manual-labeling.md): marking the pupil or anything else frame by
  frame, with the tool in the Tools menu.
- [Plugins](plugins.md): recommended plugins, such as OpenIrisDPI, and how to install them.
- [Hardware](hardware.md): cameras, IR filters and illuminators, synchronization, 3D-printed parts.

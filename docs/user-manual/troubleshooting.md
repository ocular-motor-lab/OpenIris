# Troubleshooting

Check the **Log** tab first: most problems leave a message there. The log is also saved to a file,
`OpenIrisLog-<date>.Log`, in the OpenIris folder (**Tools > Open log folder**), and each recording
has its own log.

## The eye tracking system I need isn't in the list

- **Versions before 0.1.6:** Windows blocked the plugins downloaded with OpenIris. Unblock them (see
  [Installation](installation.md#versions-before-016-unblock-the-plugins)) and restart OpenIris.
- Check the Log tab for errors loading plugins. A plugin built for a different version of OpenIris
  may fail to load.

## The cameras don't start

- **FLIR cameras:** check that the Spinnaker SDK is installed and that the cameras work in SpinView.
  Close SpinView before starting OpenIris: a camera can only be used by one program at a time.
- **Spinnaker Dual Camera** needs the two cameras connected with the synchronization cable (see
  [Hardware](hardware.md#synchronized-cameras)).
- Check the camera serial numbers in the [configuration](configuration.md#system-settings).
- The log shows the error from the camera.

## OpenIris drops frames

Watch the status bar and the red or yellow bars in the bottom corners of the window. To process
faster:

- set **Debug** to False;
- lower the frame rate, or turn off torsion (**CalculateTorsion**);
- set **MaxNumberOfProcessingThreads** to 2;
- close other programs that use the processor.

With **RecordVideo** on, the disk must also be fast enough: use a local SSD, not a network folder.

## "Error starting WEB service" in the log

This only affects controlling OpenIris from a web browser. See
[Remote control](remote-control.md#from-a-web-browser) to allow it, or ignore it: the other ways to
control OpenIris remotely work without it.

## My program can't connect to OpenIris

See [Remote control](remote-control.md): check the ports, that Windows Firewall allows them on the
OpenIris computer, and that your program doesn't wait for a reply after commands that have none.

## OpenIris closed without warning

Look at the end of the log file for a line starting with `FATAL ERROR:`, and send it to us with a
description of what you were doing. If there is no such line, Windows can save a crash report: see
[When OpenIris crashes](../plugin-development/contributing.md#when-openiris-crashes).

## Reporting a problem

Open an issue at
[github.com/ocular-motor-lab/OpenIris/issues](https://github.com/ocular-motor-lab/OpenIris/issues),
with the OpenIris version (**Help > About**), the eye tracking system, what you did, and the log.

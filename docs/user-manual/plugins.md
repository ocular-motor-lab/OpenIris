# Plugins

Plugins add eye tracking systems (cameras), tracking pipelines or calibrations to OpenIris. Some
come with OpenIris; others are developed separately and installed on top of it.

## Recommended plugins

### OpenIrisDPI: dual Purkinje image eye tracking

[OpenIrisDPI](https://github.com/ryan-ressmeyer/OpenIrisDPI), by Ryan Ressmeyer, adds a tracking
pipeline for **dual Purkinje image (DPI)** eye tracking, which measures eye rotation very precisely
from the reflections of the cornea and of the back of the lens. It runs at up to 500 Hz with two
FLIR cameras, and is used for human and non-human primate research.

Its [wiki](https://github.com/ryan-ressmeyer/OpenIrisDPI/wiki) describes several hardware
configurations with parts lists and assembly instructions, how to install and set up the software,
and how to synchronize the data with other equipment.

## Installing a plugin

1. Download the plugin (usually a zip with one or more `.dll` files) from its website.
2. Copy the files into the `Plugins` folder of OpenIris (next to `OpenIris.exe`).
3. Restart OpenIris. The new eye tracking systems, pipelines or calibrations appear in the
   [configuration](configuration.md).

A plugin is built for a version of OpenIris: check that it matches yours (**Help > About**). If a
plugin doesn't appear, the Log tab shows why it couldn't be loaded.

## Writing a plugin

To support a new camera, a new way of tracking, or a new calibration, see the
[plugin development manual](../plugin-development/README.md). If you have written a plugin others
could use, let us know (for example in a
[GitHub discussion](https://github.com/ocular-motor-lab/OpenIris/discussions)) and we'll list it
here.

# Building OpenIris from source

You only need this to change OpenIris itself or to debug a plugin inside it. To use OpenIris,
download a release instead (see [Installation](../user-manual/installation.md)).

## Requirements

- **Windows 10 or later.**
- **Visual Studio 2022 or 2026** (the free Community edition is fine) with the
  **.NET desktop development** workload. It includes the .NET Framework 4.8 targeting pack that
  OpenIris needs.
- **Git**, or GitHub Desktop.

You don't need any camera SDK to build: the camera libraries OpenIris compiles against are in the
repository. To *run* with FLIR cameras, install the
[Spinnaker SDK](https://www.flir.com/products/spinnaker-sdk/) as for a normal installation.

## Get the code

Clone the repository:

```
git clone https://github.com/ocular-motor-lab/OpenIris.git
```

Inside you'll find:

| Folder | Contents |
| --- | --- |
| `source\` | The code. `source\OpenIris.sln` is the solution to open. |
| `docs\` | This documentation. |
| `bin\` | Created by the build: the programs and plugins (not in git). |
| `VideoSamples\` | The sample videos for the Simulation system and the tests (not in git, see below). |

### Sample videos

The Simulation eye tracking system and some tests use two recordings, `JorgeLeft.avi` and
`JorgeRight.avi`, which are too large for git. Put them in a `VideoSamples` folder at the root of the
repository (next to `source` and `docs`).

<!-- TODO(Jorge): where to download the sample videos. -->

Without them OpenIris builds and runs normally; the Simulation system won't start, and the tests
that need the videos are skipped.

## Build and run

1. Open `source\OpenIris.sln` in Visual Studio.
2. In the toolbar, select the **Debug** configuration and the **x64** platform.
3. Make sure **OpenIris** is the startup project (right-click it in Solution Explorer,
   **Set as Startup Project**).
4. Press **F5**.

Everything is built into `bin\x64\Debug\` (or `bin\x64\Release\`):

- `OpenIris.exe`, `OpenIrisLib.dll` and the libraries they use;
- `Plugins\`: the plugin DLLs (eye tracking systems, pipelines, calibrations), which OpenIris loads at
  startup;
- when OpenIris runs from there, its settings (`EyeTrackerSettings.xml`) and its log files
  (`OpenIrisLog-*.Log`).

To build from the command line, open a **Developer PowerShell for Visual Studio** in the repository
folder and run:

```
msbuild source\OpenIris.sln -restore -p:Configuration=Release -p:Platform=x64
```

## What's in the solution

| Project | What it is |
| --- | --- |
| **OpenIrisLib** | The core: grabbing, processing, recording, calibration, remote control, the base classes for plugins, and the built-in Simulation system and pipelines. |
| **OpenIris** (`OpenIrisUI`) | The main program, `OpenIris.exe`. |
| **BasicPlugins** | Built-in plugins, for example calibrations and a simulated eye. |
| **SpinnakerInterface**, **FlyCaptureInterface**, **Micromedical**, **Interacoustics**, **OtherEyeTrackers** | Eye tracking systems for specific cameras and devices. |
| **OpenIrisRemoteClient** | The .NET client for controlling OpenIris remotely (see [Remote control](../user-manual/remote-control.md)). |
| **OpenIrisTests** | The automated tests (see [Testing](contributing.md#tests)). |
| Others | Small tools and test programs (remote console and GUI, video player, etc.). |

All projects are SDK-style projects targeting .NET Framework 4.8 for x64. The settings they share are
in two files in `source\`:

- `Directory.Build.props`: target framework, platform, C# language version, output folders.
- `Directory.Packages.props`: the version of every NuGet package (EmguCV, System.Text.Json…). Change
  versions here, not in the project files.

The version number of OpenIris comes from git, through GitVersion: it is the latest version tag plus
the commits since. A commit message containing `+semver: minor` (or `major`) moves to the next
minor (or major) version.

## Checks on GitHub

Every push and pull request is built on GitHub, in Debug and Release, and the tests are run. The
result shows next to each commit and pull request, and in the **Actions** tab of the repository.
The tests that need the sample videos are skipped there.

The workflow is in `.github/workflows/build.yml`.

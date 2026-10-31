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

### Sample videos

`sample-videos`, at the root of the repository, has two short recordings of the eyes (MP4, about 40
seconds). The build copies them to `bin\x64\<configuration>\SampleVideos`, where the Simulation eye
tracking system plays them, and the tests use them too.

The same recordings also exist in full quality, `JorgeLeft.avi` and `JorgeRight.avi` (about 335 MB
each, too large for git). They are optional: if you put both in `sample-videos` too (git ignores
them there), the build copies them next to `OpenIris.exe` and Simulation plays them instead of the
MP4s.

`sample-data` has the files OpenIris writes when processing the MP4s with the JOM pipeline, as an
example of the output.

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
| **SpinnakerInterface**, **FlyCaptureInterface**, **Micromedical** | Eye tracking systems for specific cameras and devices. |
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
The speed test is skipped there, because timing on shared build servers isn't meaningful.

The workflow is in `.github/workflows/build.yml`.

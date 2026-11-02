# Building OpenIris from source

You only need this to change OpenIris itself or to debug a plugin inside it. To use OpenIris,
download a release instead (see [Installation](../user-manual/installation.md)).

## Requirements

- **Windows 10 or later.**
- **Visual Studio 2022 or 2026** (the free Community edition is fine) with the
  **.NET desktop development** workload. It includes the .NET Framework 4.8 targeting pack that
  OpenIris needs.
- **Git**, or GitHub Desktop.

To build the plugin for FLIR cameras, install the
[Spinnaker SDK](https://www.flir.com/products/spinnaker-sdk/) as for a normal installation (with its
.NET libraries). FLIR's license doesn't allow redistributing its libraries, so they aren't in the
repository: the plugin compiles against the installed SDK, and at run time OpenIris loads
`SpinnakerNET_v140.dll` from the SDK folder in PATH, whatever version is installed. Without the SDK,
everything else builds, and the Spinnaker plugin is skipped with a warning. If the SDK is somewhere
else, pass its folder: `-p:SpinnakerSdkDir=C:\path\to\bin64\vs2015\`.

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
them there), the build copies them to `SampleVideos` too and Simulation plays them instead of the
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

- `OpenIris.exe` and `OpenIris.exe.config`;
- `lib\`: `OpenIrisLib.dll`, `OpenIrisWinForms.dll` and the libraries they use (OpenIris.exe.config
  tells .NET to look there);
- `Plugins\`: the plugin DLLs (eye tracking systems, pipelines, calibrations), which OpenIris loads at
  startup;
- `SampleVideos\`: the videos Simulation plays;
- when OpenIris runs from there, its settings (`OpenIrisSettings.xml` and `Settings\`) and `Logs\`, with its log
  files (`OpenIrisLog-*.Log`).

To build from the command line, open a **Developer PowerShell for Visual Studio** in the repository
folder and run:

```
msbuild source\OpenIris.sln -restore -p:Configuration=Release -p:Platform=x64
```

## What's in the solution

| Project | What it is |
| --- | --- |
| **OpenIrisLib** | The core: grabbing, processing, recording, calibration, remote control, the base classes for plugins, and the built-in Simulation system and pipelines. |
| **OpenIrisWinForms** | The Windows Forms parts: the controls, dialogs and tools of the main window, the calibration panels, and the base classes for plugin windows. OpenIrisLib doesn't use Windows Forms, so it can run without a Windows user interface. |
| **OpenIris** | The main program, `OpenIris.exe`. |
| **BasicPlugins** | Plugins without special hardware: the webcam and Synthetic Eyes systems. |
| **SpinnakerInterface**, **FlyCaptureInterface**, **Micromedical**, **RemoteEyeTracker** (in `FlirPlugins\`) | Eye tracking systems for FLIR (formerly Point Grey) cameras: Spinnaker, FlyCapture, and the Micromedical and remote systems built on them. Separate projects because they need the camera SDKs to build: SpinnakerInterface the Spinnaker SDK, the other three the FlyCapture SDK. Without an SDK, its plugins are skipped with a warning. |
| **OpenIrisTests** | The automated tests (see [Testing](contributing.md#tests)). |
| Others | Small tools and test programs (video player, etc.). |

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

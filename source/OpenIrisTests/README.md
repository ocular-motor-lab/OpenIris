# OpenIris tests

These tests check that the eye tracking pipelines (JOM, SimpleCentroid, SimplePupilCRBlobs) still
produce the same results after a code change. They don't need cameras.

Run them from Visual Studio's Test Explorer, or from the command line after building the solution:

```
dotnet test source/OpenIrisTests/OpenIrisTests.csproj --no-build -c Debug -p:Platform=x64
```

## What they check

- **Baselines** (`PipelineRegressionTests`): each pipeline processes a fixed set of frames and its
  output (pupil, iris, reflection, torsion, quality) must match the CSV files in `Baselines\` to
  within 0.01 (pixels, degrees). Two sets of frames are used:
  - 100 frames of a synthetic eye drawn by `SyntheticEye.cs`.
  - The first 300 frames of the sample videos in `source\SampleVideos` (the ones the Simulation
    system plays).
- **Accuracy** (`PipelineAccuracyTests`): the pupil and reflection must be found within 1.5 pixels
  of where the synthetic eye drew them.
- **Speed** (`PipelinePerformanceTests`): the JOM pipeline must not get more than 25% slower. The
  first run on each computer records a reference time in `Baselines\performance\` (not committed).
  Delete that file to record a new one. Skipped on GitHub.
- **Record and process** (`RecordAndProcessTests`): runs OpenIris without the user interface, as a
  user would. It tracks with the Simulation system, records about 1.5 seconds of raw video, then
  processes that recording with "Save processed video" and checks the processed video has the
  frames, side by side for both eyes, and that events sent during the recording are saved. Settings
  and recordings go to a temporary folder, so your settings are not touched.

## When a baseline test fails

The failure message lists the first values that changed, and the complete new output is written
to `bin\<Configuration>\actual\`. If the change was not intended, it's a regression. If it was
intended (for example, an improvement to a tracker), accept the new results by running the tests
with the environment variable `OPENIRIS_UPDATE_BASELINES=1`, then commit the updated CSV files:

```
set OPENIRIS_UPDATE_BASELINES=1
dotnet test source/OpenIrisTests/OpenIrisTests.csproj --no-build -c Debug -p:Platform=x64
set OPENIRIS_UPDATE_BASELINES=
```

The diff of the CSV files then shows exactly how results changed.

# Contributing to OpenIris

How changes to OpenIris itself are made, tested and released. See [Building](building.md) to set up
Visual Studio first.

## Making a change

1. Create a branch for the change. Don't work directly on `main`.
2. Make the change, and keep the documentation in `docs/` up to date with it.
3. Run the tests (see below), in Debug and preferably also in Release.
4. Push the branch and open a pull request on GitHub. GitHub builds it and runs the tests; the result
   shows on the pull request.
5. Once it's reviewed and the checks pass, merge it into `main`.

## Tests

The tests are in `source\OpenIrisTests`. Its [README](../../source/OpenIrisTests/README.md) explains
each test in detail. In short:

- **Baselines**: each tracking pipeline processes a fixed set of frames (a synthetic eye and the
  sample videos), and its results must match saved results exactly (within 0.01 pixels or degrees).
- **Accuracy**: the pupil and reflection must be found where the synthetic eye drew them.
- **Speed**: the JOM pipeline must not become more than 25% slower than on the same computer before.
- **Record and process**: OpenIris tracks the Simulation system, records a short video and processes
  it, like a user would.

Run them from **Test Explorer** in Visual Studio, or from the repository folder after building:

```
dotnet test source\OpenIrisTests\OpenIrisTests.csproj --no-build -c Debug -p:Platform=x64
```

Tests that need the sample videos are skipped when the `VideoSamples` folder is missing (as on
GitHub).

### When a baseline test fails

Eye tracking doesn't crash when something goes wrong: it silently produces different numbers. That's
what the baselines catch, so a failing baseline means the change altered tracking results.

- If that was **not** intended, it's a bug: look at the values listed in the failure message.
- If it **was** intended (for example an improvement to a pipeline), update the baselines and
  commit the new files with the change, saying why the results changed:

  ```
  $env:OPENIRIS_UPDATE_BASELINES = "1"
  dotnet test source\OpenIrisTests\OpenIrisTests.csproj --no-build -c Debug -p:Platform=x64
  Remove-Item Env:OPENIRIS_UPDATE_BASELINES
  ```

## Changing image processing

Image processing is tuned to reach 500 frames per second and more. Small changes can cost frame rate
or change results in ways that are hard to see. For changes there:

- check the speed test, and if possible track with real cameras at full frame rate;
- expect baseline changes, and check them against the accuracy tests;
- see [Known image processing issues](image-processing-improvements.md) for problems already found
  and how to approach them.

## Versions and releases

The version number comes from git tags (`0.1.5`, `0.2.0`…) through GitVersion: builds between
releases are numbered after the last tag. To move to a new minor or major version, include
`+semver: minor` or `+semver: major` in a commit message.

To make a release:

1. Merge everything into `main` and push. Check that the GitHub build passes.
2. Create the version tag on that commit (for example `0.2.0`) and push it.
3. Build **Release** from that commit, so the program reports exactly that version.
4. Zip the `bin\x64\Release` folder as `OpenIris_Win_Release_<version>.zip`.
5. On GitHub: **Releases**, **Draft a new release**, choose the tag, write what's new, attach the
   zip, **Publish release**.

## When OpenIris crashes

- Unhandled errors are written to the OpenIris log (`OpenIrisLog-*.Log`, next to `OpenIris.exe`) as
  a line starting with `FATAL ERROR:`, with the stack trace.
- Some crashes inside native libraries (OpenCV, camera drivers) close OpenIris before it can log
  anything. To capture those, have Windows save a crash dump: run once, in a Command Prompt opened as
  administrator:

  ```
  reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\OpenIris.exe" /v DumpFolder /t REG_EXPAND_SZ /d "C:\CrashDumps" /f
  reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\OpenIris.exe" /v DumpType /t REG_DWORD /d 1 /f
  ```

  After a crash, `C:\CrashDumps` has a `.dmp` file that Visual Studio can open to show where it
  happened.

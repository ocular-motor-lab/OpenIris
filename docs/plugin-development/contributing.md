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

## Documentation

The documentation is the Markdown files in `docs/`. On every push to `main`, GitHub builds them into
the website at [omlab.berkeley.edu/OpenIris](https://omlab.berkeley.edu/OpenIris/),
with [MkDocs](https://www.mkdocs.org/) and the Material theme (configured in `mkdocs.yml`). Pull
requests that change the documentation are checked too: a broken link fails the check.

- Keep the user manual (`docs/user-manual/`) free of programming; it is for people running
  experiments. Everything for programmers goes in `docs/plugin-development/`.
- A new page must also be added to `nav` in `mkdocs.yml` to appear in the site's menu.
- Notes in GitHub's format (`> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`) show as boxes on both GitHub
  and the site.

To preview the site while editing, with Python installed:

```
pip install -r docs/requirements.txt
mkdocs serve
```

and open <http://127.0.0.1:8000>. The page reloads when you save a file.

## Tests

The tests are in `source\OpenIrisTests`. Its [README](https://github.com/ocular-motor-lab/OpenIris/blob/main/source/OpenIrisTests/README.md) explains
each test in detail. In short:

- **Baselines**: each tracking pipeline processes a fixed set of frames (a synthetic eye and the
  sample videos), and its results must match saved results exactly (within 0.01 pixels or degrees).
- **Accuracy**: the pupil and reflection must be found where the synthetic eye drew them.
- **Speed**: the JOM pipeline must not become more than 25% slower than on the same computer before.
- **Record and process**: OpenIris tracks the Simulation system, records a short video and processes
  it, like a user would.
- **Data file**: the lines of the data file and the values streamed through LSL must not change.
- **Cameras** (category Hardware): with the two FLIR cameras of Spinnaker Dual Camera connected,
  OpenIris records 20 s at 100 Hz with raw videos and at 450 Hz, and no frame may be lost. They start
  the built `OpenIris.exe` (close OpenIris first) and skip themselves when the cameras or the
  Spinnaker plugin aren't there. Run only them with `--filter TestCategory=Hardware`; GitHub leaves
  them out.

Run them from **Test Explorer** in Visual Studio, or from the repository folder after building:

```
dotnet test source\OpenIrisTests\OpenIrisTests.csproj --no-build -c Debug -p:Platform=x64
```

The speed test is skipped on GitHub, where timing isn't meaningful.

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

- Unhandled errors are written to the OpenIris log (`OpenIrisLog-*.Log`, in the `Logs` folder next to `OpenIris.exe`) as
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

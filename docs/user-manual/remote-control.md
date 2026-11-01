# Controlling OpenIris from other programs

OpenIris can be controlled over the network while it runs, from the same computer or another one.
A stimulus program (MATLAB, Python, PsychoPy, Psychtoolbox…) can start and stop recordings, mark
events in the data, read the latest eye position, and drive a calibration.

There are three ways to do it:

- **A web browser.** Open `http://<OpenIris computer>:9001/` on any computer, tablet or phone of the
  network to see the eyes and a live trace, record, change settings, and run a full-screen
  calibration. See [Web client](#web-client).
- **The web API** (HTTP and JSON), used by the web client and by the MATLAB class
  `OpenIrisMatlabClient.m`. Any language can use it. See [Web API](#web-api).
- **Text commands over UDP or TCP.** Simple text messages, the fastest for events during an
  experiment. Replies with eye data are JSON. See [Text commands](#text-commands-udp-and-tcp).

OpenIris listens as soon as it starts; tracking does not need to be running. The log at startup
shows which services started, for example `UDP server Waiting for requests in port 9003.`

## Ports

All ports are numbered from the setting **ServiceListeningPort** (Configuration, General settings,
section G, default 9000). Changing it requires restarting OpenIris.

| Port | Default | Used for |
| --- | --- | --- |
| ServiceListeningPort | 9000 | Not used (was the .NET remote service, removed in 0.2) |
| ServiceListeningPort + 1 | 9001 | Web client and web API (`http`), see [Web client](#web-client) |
| ServiceListeningPort + 2 | 9002 | Text commands over **TCP** |
| ServiceListeningPort + 3 | 9003 | Text commands over **UDP** |

To control OpenIris from another computer, Windows Firewall on the OpenIris computer must allow these
ports. Windows usually asks the first time OpenIris runs.

## Text commands (UDP and TCP)

A command is a plain ASCII string. Commands that take a parameter separate it with a vertical bar:
`COMMAND|parameter`. Command names are not case sensitive.

| Command | Reply | What it does |
| --- | --- | --- |
| `STARTRECORDING` | none | Starts recording, with the current session name and settings. |
| `STOPRECORDING` | none | Stops recording. |
| `SETSESSIONNAME\|name` | none | Changes the session name used for the next recording. |
| `RECORDEVENT\|message` | frame number | Saves `message` in the events file of the current recording, with the current frame number, and replies with that frame number. Replies `0` if not tracking. |
| `GETDATA` | JSON | Replies with the data of the last processed frame. |
| `WAITFORDATA` | JSON | Waits for the next processed frame (up to 1 second) and replies with its data. |
| `CALIBRATION\|message` | text | Sends a message to the calibration in progress. See [Calibration](#calibration). |
| `TESEMPTY` | `D` | Test command: replies with the single character `D`. |

> [!IMPORTANT]
> Commands marked "none" send **no reply**. Don't wait for an answer after them, or your program
> will hang (or time out).

A command OpenIris doesn't recognize, or one that fails, gets no reply and an error is written in the
log. (Before version 0.1.6, it stopped the UDP or TCP listener until OpenIris was restarted.)

### UDP or TCP?

- **UDP (port 9003)** is the simplest: send one message, read one reply. Each message is independent,
  and the reply goes back to the address and port the command came from. Use this unless you have a
  reason not to.
- **TCP (port 9002)** keeps a connection open. OpenIris serves one connection at a time. TCP does not
  separate messages, so send one command and read its reply before sending the next one.

### The data reply (GETDATA, WAITFORDATA)

The reply is a JSON object with one entry per eye, and the system's extra data:

```json
{
  "Left": {
    "FrameNumber": 12345,
    "Seconds": 102.583,
    "Pupil": {
      "Center": { "IsEmpty": false, "X": 161.2, "Y": 118.7 },
      "Size": { "IsEmpty": false, "Width": 41.3, "Height": 39.8 },
      "Angle": 12.5
    },
    "CRs": [ { "IsEmpty": false, "X": 158.1, "Y": 125.0 } ]
  },
  "Right": { "...": "same fields as Left" },
  "Extra": { "Int0": 0, "Int1": 0, "...": "Int0 to Int9, Double0 to Double8" }
}
```

- **FrameNumber**: the camera's own frame number for this image.
- **Seconds**: the time the image was taken, in seconds, from the camera clock when the camera
  provides one.
- **Pupil**: the pupil ellipse, in pixels of the eye image: center, size (width and height) and angle
  in degrees.
- **CRs**: the centers of the corneal reflections found, in pixels. Empty if none were found.
- **Extra**: values that depend on the eye tracking system. With the Spinnaker systems, `Int0` and
  `Int1` hold the status of the camera's input lines for the left and right camera (see
  [Synchronizing with other hardware](hardware.md#synchronizing-with-other-hardware)).

If there is no data (not tracking, or an eye that isn't tracked), the values are zero.

Positions are raw image coordinates, not calibrated gaze. They are meant for monitoring the subject
or gaze-contingent experiments in image space.

### Example: Python

```python
import json
import socket

OPENIRIS = ("127.0.0.1", 9003)  # Address of the OpenIris computer, UDP port

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.settimeout(2.0)

def send(command):
    """Sends a command that has no reply."""
    sock.sendto(command.encode("ascii"), OPENIRIS)

def ask(command):
    """Sends a command and returns its reply."""
    sock.sendto(command.encode("ascii"), OPENIRIS)
    reply, _ = sock.recvfrom(65535)
    return reply.decode("ascii")

send("SETSESSIONNAME|Subject01")
send("STARTRECORDING")

frame = int(ask("RECORDEVENT|StimulusOn"))
data = json.loads(ask("GETDATA"))
print("Frame", frame, "left pupil at", data["Left"]["Pupil"]["Center"]["X"], data["Left"]["Pupil"]["Center"]["Y"])

send("STOPRECORDING")
```

### Example: MATLAB

Uses `udpport` (MATLAB R2020b or later).

```matlab
openiris = "127.0.0.1";   % Address of the OpenIris computer
port = 9003;              % UDP port
u = udpport;

send = @(command) write(u, uint8(char(command)), "uint8", openiris, port);

send("SETSESSIONNAME|Subject01");
send("STARTRECORDING");

data = jsondecode(ask(u, "GETDATA", openiris, port));
fprintf("Left pupil at %.1f, %.1f\n", data.Left.Pupil.Center.X, data.Left.Pupil.Center.Y);

send("STOPRECORDING");

function reply = ask(u, command, openiris, port)
    % Sends a command and waits up to 2 seconds for the reply.
    flush(u);
    write(u, uint8(char(command)), "uint8", openiris, port);
    t = tic;
    while u.NumBytesAvailable == 0 && toc(t) < 2
        pause(0.001);
    end
    reply = char(read(u, u.NumBytesAvailable, "uint8"));
end
```

### MATLAB class for TCP

`OpenIrisTCP.m` (in the [`matlab`](https://github.com/ocular-motor-lab/OpenIris/tree/main/matlab) folder of the repository) wraps the commands over TCP, including waiting
for complete replies:

```matlab
oi = OpenIrisTCP;
oi.Connect('127.0.0.1');            % Port 9002 by default
oi.SetSessionName('Subject01');
oi.StartRecording();
frame = oi.RecordEvent('StimulusOn');
data = oi.GetCurrentData();         % Or oi.WaitForData() for the next frame
fprintf('Left pupil at %.1f, %.1f\n', data.LeftX, data.LeftY);
oi.StopRecording();
```

The data has the whole reply in `data.Left`, `data.Right` and `data.Extra`, and for convenience
`LeftX`, `LeftY`, `LeftTime`, `LeftFrameNumber` and the same for the right eye.

## Calibration

A calibration can be driven by the stimulus program when the **Calibration method** is
**Remote Linear** (Configuration, General settings, section C):

1. Start the calibration in OpenIris (the **Calibrate** button).
2. For each calibration target, the stimulus program shows the target and sends
   `CALIBRATION|FixStart:x,y`, with the position of the target, while the subject looks at it. Then
   it sends `CALIBRATION|FixStop`. Use `CALIBRATION|FixAbort` instead to discard that target (for
   example after a blink).
3. When all targets are done, it sends `CALIBRATION|Complete`. OpenIris fits a linear transformation
   from eye position to target position. Each target counts as one point: the median of the data
   collected during it.

Other messages:

| Message | Effect |
| --- | --- |
| `SetSource:n` | Which signal is used as eye position. |
| `SetRef:n` | Which signal is subtracted as reference. |

For both, `n` is 0 = none, 1 = pupil, 2 to 5 = corneal reflection 1 to 4. The defaults are pupil
for the source and the first corneal reflection for the reference.

Each message replies `ack`, or an error message if it couldn't be understood. With no calibration in
progress, messages are ignored and reply `ack`.

To synchronize eye data with a stimulus display, see also
[OpenIrisSync](https://github.com/ocular-motor-lab/OpenIrisSync/wiki).

## Web client

Open `http://localhost:9001/` on the OpenIris computer, or `http://<its address>:9001/` from another
one (**Show IP / network info** in the OpenIris menus gives the address). The page shows:

- Start and stop tracking and recording, the session name, and events.
- The image of each eye with the tracking drawn on top, as in OpenIris, and the pupil and
  reflection thresholds with buttons to change them.
- The calibrated position of each eye and a **live trace** of horizontal, vertical and torsion
  (also pupil size and position, and head velocity), with every frame, however fast the camera.
- Calibrate, cancel and reset the reference, and the files of the last recording, to download.
- All the settings, as in the settings window. Changes apply right away.

Several browsers can be open at the same time.

### Full-screen calibration and validation

**Full-screen calibration** opens a page that shows calibration targets on the screen the subject
looks at. Open it in a browser on the computer of that screen, choose the display (or move the
window to it), and press **Calibrate**. The targets go full screen; the subject looks at the center of each one. Esc
stops at any time.

Choosing the display works in Chrome and Edge, which ask once for permission (**Find displays**),
and only when the page is opened as `http://localhost:9001`, on the OpenIris computer. Browsers don't
allow it for pages at another computer's address: there, move the window to the display instead.

It uses the **Remote Linear** calibration (by Ryan Ressmeyer, see [Calibration](#calibration)) and
switches OpenIris to it. Set the width of the screen and the viewing distance, so the targets are at
the right angles, and the number of targets (5, 9 or 13), how far out they go, and how long each
lasts. The positions are in degrees, up and right positive. At the end the subject looks at the
center target, which becomes the zero reference.

The result shows each target with the position the calibration gives for each eye. **Validate**
shows the targets again (in a new random order) and compares them with the gaze: the mean and
maximum error of each eye, in degrees, is the accuracy to report. The calibration error is lower than
the real one, because the fit was made to those same points.

## Web API

The web client uses this API, and any program can too. Replies are JSON. A command that can't run
replies with an error status and `{"error": "message"}`: 409 when it isn't possible now (for example
recording while not tracking), 404 when something doesn't exist, 400 for a bad request. Values that
don't exist, such as the position of an eye that wasn't found, are `null`.

| Request | Does |
| --- | --- |
| `GET /api/status` | Tracking, recording, calibrating, frame rate, system, session name, version... |
| `POST /api/tracking/start`, `/api/tracking/stop` | Start or stop tracking |
| `POST /api/recording/start`, `/api/recording/stop` | Start or stop recording |
| `POST /api/events` | Save the body (text) as an event; replies `{"FrameNumber": n}` |
| `GET /api/data` | Data of the last frame: `Left`, `Right` (raw), `LeftCalibrated`, `RightCalibrated`, `Head`, `Extra` |
| `GET /api/data/next` | The same, for the next frame (waits up to 1 s) |
| `GET /api/images/left`, `/api/images/right` | Last image of the eye (JPEG), with the tracking drawn; `?overlay=false` without |
| `GET /api/settings` | All the settings, with their group, description, type, value and options |
| `PUT /api/settings/{name}` | Change a setting; the body is the value (JSON or text). Names as in the list: `SessionName`, `Tracking.DarkThresholdLeftEye`... |
| `GET /api/profiles` | The [settings profiles](configuration.md#settings-profiles): `Active` and `Profiles` |
| `PUT /api/profiles/active` | Switch to another profile; the body is its name. Only while not tracking |
| `POST /api/profiles` | Save the current settings as a new profile, which becomes the active one; the body is its name |
| `POST /api/reference/reset` | Reset the reference |
| `POST /api/calibration/start`, `/cancel`, `/reset` | Start, cancel, or reset the calibration |
| `POST /api/calibration/message` | Send the body (text) to the calibration, for example `FixStart:5,0`; replies `{"Reply": "ack"}` |
| `GET /api/files/last` | Names of the files of the last recording |
| `GET /api/files/{name}` | Download a file of the last recording (or of the data folder) |

From Python, for example:

```python
import requests
oi = "http://localhost:9001/api"
requests.put(oi + "/settings/SessionName", json="Subject01")
requests.post(oi + "/recording/start")
frame = requests.post(oi + "/events", data="StimulusOn").json()["FrameNumber"]
data = requests.get(oi + "/data").json()
print(data["LeftCalibrated"]["HorizontalPosition"])
requests.post(oi + "/recording/stop")
```

### Live data

The WebSocket `ws://<address>:9001/ws/data` sends every frame. First it sends the names of the
columns, which are those of the [data file](data-file.md):
`{"type": "columns", "columns": ["LeftFrameNumber", ...]}`. Then, about 30 times a second, the
frames since the last message: `{"type": "data", "rows": [[...], [...]]}`.

### MATLAB

`OpenIrisMatlabClient.m` (in the [`matlab`](https://github.com/ocular-motor-lab/OpenIris/tree/main/matlab) folder) uses the web API. It needs no DLL
and works on any system (MATLAB R2016b or later):

```matlab
oi = OpenIrisMatlabClient;
oi.Connect('127.0.0.1');               % Or Connect() to use the address in openiris_folder\IP.txt
oi.SetSessionName('Subject01');
oi.StartRecording();
frame = oi.RecordEvent('StimulusOn');
data = oi.GetCurrentData();             % data.LeftCalibrated.HorizontalPosition, data.Left.Pupil.Center.X...
oi.StopRecording();
files = oi.DownloadFile();              % Into openiris_folder\DownloadedData
```

With Psychtoolbox installed, `RecordEvent` starts each message with the `GetSecs` time, to help
synchronize the stimulus and eye tracker clocks. Also: `IsRecording`, `GetStatus`,
`ChangeSetting(name, value)`, `WaitForNewData`, `ResetReference`, `StartCalibration`,
`CalibrationMessage`, `CancelCalibration`.

Scripts written for the old version of this class, which used `OpenIrisRemoteClient.dll` and the
.NET remote service (removed in 0.2), keep working. The difference: `GetCurrentData` returns a
struct (`data.Left.Pupil.Center.X`) instead of a .NET object (`data.Item(0)...`).

The URLs of the old browser commands (`http://localhost:9001/EyeTrackerEndpoint/Web/StartRecording`,
`StopRecording`, `ResetReference`, `RecordEvent?message=...`, `GetFileWeb`, `StatusSummary`) also
still work, and no longer need the `netsh http add urlacl` command.

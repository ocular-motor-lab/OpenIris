# Controlling OpenIris from other programs

OpenIris can be controlled over the network while it runs, from the same computer or another one.
A stimulus program (MATLAB, Python, PsychoPy, Psychtoolbox…) can start and stop recordings, mark
events in the data, read the latest eye position, and drive a calibration.

There are two ways to do it:

- **Text commands over UDP or TCP** (recommended). Simple text messages that any language can send.
  Replies with eye data are JSON.
- **The .NET remote service**, used by `OpenIrisRemoteClient.dll` and the MATLAB class
  `OpenIrisMatlabClient.m`. It gives access to a few more functions, but only works from .NET or
  MATLAB on Windows.

OpenIris listens as soon as it starts; tracking does not need to be running. The log at startup
shows which services started, for example `UDP server Waiting for requests in port 9003.`

## Ports

All ports are numbered from the setting **ServiceListeningPort** (Configuration, General settings,
section G, default 9000). Changing it requires restarting OpenIris.

| Port | Default | Used for |
| --- | --- | --- |
| ServiceListeningPort | 9000 | .NET remote service (`net.tcp`), used by `OpenIrisRemoteClient.dll` |
| ServiceListeningPort + 1 | 9001 | Web service (`http`), see [Browser](#from-a-web-browser) |
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
log. (Before version 0.2.0, it stopped the UDP or TCP listener until OpenIris was restarted.)

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

`OpenIrisTCP.m` (in `source\EyeTrackerRemoteClient`) wraps the commands over TCP, including waiting
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

## The .NET remote service

`OpenIrisRemoteClient.dll` (in the OpenIris folder) connects to port 9000. Its class
`OpenIris.OpenIrisClient(hostname, port)` offers, among others: `StartRecording`, `StopRecording`,
`RecordEvent`, `GetCurrentData`, `WaitForNewData`, `ChangeSetting(name, value)`, `ResetReference`,
the current status, and downloading recorded files.

```csharp
var openIris = new OpenIris.OpenIrisClient("127.0.0.1", 9000);
openIris.ChangeSetting("SessionName", "Subject01");
openIris.StartRecording();
long frame = openIris.RecordEvent("StimulusOn");
openIris.StopRecording();
```

From MATLAB on Windows, `OpenIrisMatlabClient.m` (in `source\EyeTrackerRemoteClient`) wraps it:

```matlab
oi = OpenIrisMatlabClient;
oi.openiris_folder = 'C:\OpenIris';   % Folder with OpenIrisRemoteClient.dll
oi.Connect('127.0.0.1');
oi.SetSessionName('Subject01');
oi.StartRecording();
frame = oi.RecordEvent('StimulusOn');
oi.StopRecording();
```

With Psychtoolbox installed, `RecordEvent` starts each message with the `GetSecs` time, to help
synchronize the stimulus and eye tracker clocks.

## From a web browser

The web service (port 9001) accepts simple commands from a browser, for example
`http://localhost:9001/EyeTrackerEndpoint/Web/StartRecording`.

Windows only lets programs open this kind of port with permission. If the log shows
`Error starting WEB service`, run this once in a Command Prompt opened as administrator (replace
`username` with the Windows user that runs OpenIris), then restart OpenIris:

```
netsh http add urlacl url=http://+:9001/EyeTrackerEndpoint/ user=username
```

The other services don't need this.

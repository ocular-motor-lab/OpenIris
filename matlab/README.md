# MATLAB tools for OpenIris

Put this folder on the MATLAB path.

| File | What it is |
| --- | --- |
| `+OpenIris/OpenIrisClient.m` | `OpenIris.OpenIrisClient`: controls OpenIris over HTTP (the web API), TCP or UDP: recording, events, data, settings, calibration, downloading files. Works on any system, no DLL needed. Replaces the old `OpenIrisRemoteClient.dll`, with the same name and methods. |
| `OpenIrisMatlabClient.m` | The same over HTTP, with `Connect(ip, port)`. Kept for existing scripts. |
| `OpenIrisTCP.m` | The same over TCP, with `Connect(ip, port)`. Kept for existing scripts. |

How to use them, and the commands and data they give: [Remote control](../docs/user-manual/remote-control.md#matlab).
To read the data files: [The data file](../docs/user-manual/data-file.md).

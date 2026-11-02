# OpenIris

Libraries for writing plugins for [OpenIris](https://github.com/ocular-motor-lab/OpenIris), an
open-source eye tracker: eye tracking systems (cameras), tracking pipelines and calibrations.

- **OpenIrisLib**: the core, with the base classes for plugins.
- **OpenIrisWinForms**: the Windows Forms parts, for plugins with their own windows.

Use the version of the OpenIris you will run the plugin in, and exclude the runtime assets: OpenIris
provides these libraries (and EmguCV) at run time, so they must not be copied next to the plugin.

```xml
<PackageReference Include="OpenIrisLib" Version="0.2.0" ExcludeAssets="runtime" />
```

See [Creating a plugin](https://github.com/ocular-motor-lab/OpenIris/blob/main/docs/plugin-development/creating-a-plugin.md).
OpenIris is licensed under the GNU Affero General Public License v3.0.

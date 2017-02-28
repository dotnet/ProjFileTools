# Project File Tools

<!-- Replace this badge with your own-->
[![Build status](https://ci.appveyor.com/api/projects/status/hv6uyc059rqbc6fj?svg=true)](https://ci.appveyor.com/project/madskristensen/extensibilitytools)

<!-- Update the VS Gallery link after you upload the VSIX-->
Download this extension from the [VS Gallery](https://visualstudiogallery.msdn.microsoft.com/[GuidFromGallery])
or get the [CI build](http://vsixgallery.com/extension/ProjPackageIntellisense.Mike Lorbetske.e22f3d7e-6b01-4ef8-b3cb-ea293a87b00f/).

---------------------------------------

Provides Intellisense and other tooling for XML based project files such as .csproj and .vbproj files.

See the [change log](CHANGELOG.md) for changes and road map.

## Features

- Intellisense for NuGet package name and version
- Hover tooltips

### Intellisenes
Full Intellisense for NuGet package references is provided for both packages that are locally cached as well as packages defined in any feed - local and online.

![Tooltip](art/completion-name.png)

![Tooltip](art/completion-version.png)

### Hover tooltips
Hovering over a package reference shows details about that package.

![Tooltip](art/tooltip.png)

## Contribute
Check out the [contribution guidelines](CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[MIT](LICENSE)

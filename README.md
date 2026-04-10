![Ra3 Tools 100px](https://user-images.githubusercontent.com/17956756/148715792-5db21b5b-5a81-4b29-b88a-478e863cb8b1.png)

# Ra3.Tools

[English](README.md) | [简体中文](README.zh-CN.md)

Windows-only RA3 SDK for local installation management, launch helpers, SkuDef editing, 4GB patching, display advice, and file utilities.

## Features

- RA3 installation validation and known path discovery
- Registry read, repair, and language switching
- Launch helpers for normal, browser, and windowed modes
- SkuDef load/save with raw line preservation
- 4GB patch enable/disable for `ra3_1.12.game`
- Display mode enumeration and RA3 resolution recommendation
- Shared file access and CRC32C checksum helpers

## Package

```powershell
dotnet add package Ra3.Tools
```

## Examples

### Read install path

```csharp
using Ra3.Tools.Installation;

var registry = new Ra3Registry();
var installPath = registry.GetInstallPath();
var installation = new Ra3Installation(installPath);

if (Ra3InstallationValidator.HasRequiredGameFiles(installation.RootPath))
{
    Console.WriteLine(installation.GameDataExecutablePath);
}
```

### Rebuild registry

```csharp
using Ra3.Tools.Installation;

var registry = new Ra3Registry();
registry.Rebuild(@"D:\Games\Red Alert 3", "YOUR-CD-KEY");
registry.TrySetLanguage(@"D:\Games\Red Alert 3", "english");
```

### Edit SkuDef

```csharp
using Ra3.Tools.Configuration;

var document = SkudefSerializer.Load(@"D:\Games\Red Alert 3\example.skudef");
var updated = document with
{
    Entries = document.Entries
        .Concat(new SkudefEntry[]
        {
            new AddBigEntry(@"D:\Mods\example.big")
        })
        .ToArray()
};

SkudefSerializer.Save(@"D:\Games\Red Alert 3\example.skudef", updated);
```

### Toggle 4GB patch

```csharp
using Ra3.Tools.Patching;

var patchService = new Ra3LargeAddressPatchService();
var gamePath = @"D:\Games\Red Alert 3\Data\ra3_1.12.game";

if (!patchService.IsEnabled(gamePath))
{
    patchService.Enable(gamePath);
}
```

### Get recommended display mode

```csharp
using Ra3.Tools.Graphics;

var displayService = new WindowsDisplayService();
var supported = displayService.GetSupportedDisplayModes();
var current = displayService.GetCurrentDisplayMode();
var recommended = Ra3DisplayAdvisor.GetRecommendedDisplayMode(supported, current);
```

## Namespaces

- `Ra3.Tools.Installation`
- `Ra3.Tools.Configuration`
- `Ra3.Tools.Graphics`
- `Ra3.Tools.IO`
- `Ra3.Tools.Patching`
- `Ra3.Tools.Versioning`

## Notes

- Target framework: `net8.0-windows7.0`
- This package only contains local RA3 tooling. It does not include updater, downloader, platform networking, or product-specific mod logic.

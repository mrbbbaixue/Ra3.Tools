![Ra3 Tools 100px](https://user-images.githubusercontent.com/17956756/148715792-5db21b5b-5a81-4b29-b88a-478e863cb8b1.png)

# Ra3.Tools

[English](README.md) | [简体中文](README.zh-CN.md)

Windows-only RA3 SDK for local installation management, launch helpers, SkuDef editing, 4GB patching, display advice, and file utilities.

## Features

- RA3 installation validation and known path discovery
- Registry read, repair, and language switching
- Launch helpers for normal, browser, and windowed modes, with optional SkuDef config
- SkuDef load/save with raw line preservation
- 4GB patch enable/disable for `ra3_1.12.game`
- Display mode enumeration and RA3 resolution recommendation
- Map package (zip) installation with Zip Slip protection
- Steam AppID generation (`steam_appid.txt`)
- RA3 online profile parsing (GameSpyLogin.ini accounts with password decryption, ProfileData.ini player ID)
- Shared file access and CRC32C checksum helpers
- RA3 version parsing and matching (`1.12`, `1.13`, channel letters like `4.02N`)

## Package

```powershell
dotnet add package Ra3.Tools
```

### 2.1.5 window placement fix

`Ra3GameLauncher` now waits for the **same valid game window to stop responding during startup and then recover** before applying windowed position/z-order adjustments. A first responsive window, input-idle notification, fixed sleep, or temporarily stable size is not startup completion. Missing, destroyed, or replacement windows reset the observation. `TryFindWindow` retains its default 5-second timeout and returns false on exit/timeout; a nonpositive timeout remains unlimited (as used by the launcher). If the startup unresponsive phase is never observed, it does not move the window. Call the launcher from a worker thread because this wait is synchronous. Shell-based `EnsureUnelevated` launches still return no process handle and do not adjust windows.

The 2.1.4 DPI-unaware, actual-window-size centering remains unchanged, including explicit `-x`/`-y` and `-allowAlwaysOnTop`. The subsequent bounded size-stabilization pass is only a safeguard after readiness, not a substitute for it. No monitor-selection API is added.

Compared upstream sources: [mrbbbaixue/fix/window-position-dpi](https://github.com/mrbbbaixue/RA3Bar-RA3Launcher/blob/b1df27d787db733366ab6a530b91a7fdd64b3811/main.cpp) already uses DPI-unaware, actual-size/monitor centering; [lanyizi/master](https://github.com/lanyizi/RA3Bar-RA3Launcher/blob/8b0b9bd341649de27281f83d8cd3d506fb690a8b/main.cpp) uses desktop dimensions and `-xres`/`-yres`. Both accept the first successful `WM_NULL` probe after input-idle; neither explicitly observes startup unresponsiveness and recovery. Version 2.1.5 addresses that remaining timing gap rather than duplicating the DPI fix.

Regression coverage in `Ra3GameWindowTests` exercises early/repeated responsiveness, recovery, window replacement/disappearance, and centering with the recovered size. Real-game verification additionally requires Windows, an installed RA3 with its matching SkuDef/data executable, and a startup that exposes the unresponsive phase. Launch with `LaunchMode.Windowed`, `Arguments: "-fullscreen -xres <width> -yres <height>"`, and `EnsureUnelevated: false`; observe that placement happens only after recovery and compare window/monitor rectangles in the same DPI-unaware context. Also check explicit coordinates, process exit during startup, and a finite `TryFindWindow` timeout. These checks do not require editing game/profile files.

Verified on Windows: all 128 Release tests passed. An actual game launched with `-win -fullscreen -xres 1280 -yres 720` was positioned after recovery; in the DPI-unaware coordinate space its 1024×576 window ended at `(512, 288)`, matching the center computed from the monitor bounds. Multi-monitor behavior was not verified on this single-monitor machine; monitor selection is tracked separately in [issue #21](https://github.com/mrbbbaixue/Ra3.Tools/issues/21).

Release tooling uses `Microsoft.SourceLink.GitHub` 10.0.402 to avoid the transitive `Microsoft.Build.Tasks.Git` advisory [GHSA-23fw-v26w-5fgq](https://github.com/advisories/GHSA-23fw-v26w-5fgq), which otherwise blocks restore with warnings treated as errors. Publishing still follows `.github/workflows/publish.yml`: restore/build/test on Windows with .NET 10, pack, then NuGet trusted publishing from a `v*` tag (or a manual workflow dispatch).

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

### Install map package

```csharp
using Ra3.Tools.IO;
using Ra3.Tools.Installation;

var installed = ZipMapInstaller.InstallMapPackage(@"D:\Downloads\map_pack.zip", Ra3KnownPaths.MapsDirectoryPath);
```

### Generate Steam AppID

```csharp
using Ra3.Tools.Installation;

if (Ra3InstallationValidator.NeedsSteamAppId(@"D:\Games\Red Alert 3"))
{
    Ra3InstallationValidator.GenerateSteamAppId(@"D:\Games\Red Alert 3");
}
```

### Parse online profile data

```csharp
using Ra3.Tools.Profiles;

var login = GameSpyLoginDocument.Load(@"%appdata%\Red Alert 3\Profiles\PlayerOne\GameSpyLogin.ini");
foreach (var account in login.Accounts)
{
    Console.WriteLine($"{account.Email} / {account.Nickname} / {account.DecryptPassword()}");
}

var identity = Ra3ProfileData.Load(@"%appdata%\Red Alert 3\Profiles\PlayerOne\ProfileData.ini");
Console.WriteLine(identity.PublicKey); // player ID (PPK)
```

## Namespaces

- `Ra3.Tools.Installation`
- `Ra3.Tools.Configuration`
- `Ra3.Tools.Graphics`
- `Ra3.Tools.IO`
- `Ra3.Tools.Patching`
- `Ra3.Tools.Profiles`
- `Ra3.Tools.Versioning`

## Notes

- Target framework: `net10.0-windows7.0`
- This package only contains local RA3 tooling. It does not include updater, downloader, platform networking, or product-specific mod logic.

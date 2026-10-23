![Ra3 Tools 100px](https://user-images.githubusercontent.com/17956756/148715792-5db21b5b-5a81-4b29-b88a-478e863cb8b1.png)

# Ra3.Tools

[English](README.md) | [简体中文](README.zh-CN.md)

一个仅面向 Windows 的 RA3 SDK，提供本地安装管理、启动辅助、SkuDef 读写、4GB Patch、显示推荐和文件工具。

## 功能

- 检测 RA3 安装目录并定位常用路径
- 读取、修复注册表并切换语言
- 支持普通启动、游戏大厅启动和窗口化启动，可附带 SkuDef 配置
- 读取和写回 SkuDef，同时保留未知原始行
- 为 `ra3_1.12.game` 启用或关闭 4GB Patch
- 枚举显示模式并给出适合 RA3 的推荐分辨率
- 地图 zip 包安装（防 Zip Slip）
- 生成 Steam AppID（steam_appid.txt）
- 解析 RA3 在线档案（GameSpyLogin.ini 账号与密码解密、ProfileData.ini 游戏 ID）
- 提供共享文件读取和 CRC32C 校验工具
- 解析和匹配 RA3 版本号（1.12、1.13，及 4.02N 等渠道版本）

## 安装

```powershell
dotnet add package Ra3.Tools
```

### 2.1.5 窗口定位修复

`Ra3GameLauncher` 现在必须观察到**同一个有效游戏窗口在启动时无响应，再恢复响应**，才调整窗口化位置和置顶状态。首次响应、输入空闲、固定延迟或尺寸暂时不变，都不能代替真实启动阶段。窗口消失、销毁或替换时重置观察。`TryFindWindow` 保留默认 5 秒超时，进程退出或超时返回 false；超时参数 ≤ 0 仍为无限等待（启动器使用此模式）。未观察到启动无响应阶段时不移动窗口。等待为同步过程，调用方应在工作线程启动。通过 shell 非提权启动（`EnsureUnelevated`）仍拿不到进程句柄，因此不调整窗口。

2.1.4 已有的 DPI-unaware、按实际窗口尺寸居中、`-x`/`-y` 显式定位和 `-allowAlwaysOnTop` 语义保持不变。就绪后的有界尺寸稳定观察只是额外保障，不再承担判断启动完成的职责。本次不添加显示器选择 API。

实际对比源码：[mrbbbaixue/fix/window-position-dpi](https://github.com/mrbbbaixue/RA3Bar-RA3Launcher/blob/b1df27d787db733366ab6a530b91a7fdd64b3811/main.cpp) 已采用 DPI-unaware、实际尺寸和所在显示器居中；[lanyizi/master](https://github.com/lanyizi/RA3Bar-RA3Launcher/blob/8b0b9bd341649de27281f83d8cd3d506fb690a8b/main.cpp) 按桌面与 `-xres`/`-yres` 计算。两者都是输入空闲后首次成功响应 `WM_NULL` 就定位，并未显式观察启动无响应到恢复的转换。2.1.5 修复的是这一剩余时序缺口，不是重复 DPI 修复。

`Ra3GameWindowTests` 覆盖过早/持续响应、无响应后的恢复、窗口替换/消失，以及按恢复后的尺寸居中。真实游戏验证还需要 Windows、带匹配 SkuDef/数据可执行文件的 RA3 安装和会出现无响应阶段的启动：使用 `LaunchMode.Windowed`、`Arguments: "-fullscreen -xres <宽> -yres <高>"`、`EnsureUnelevated: false`，观察是否只在恢复响应后定位，并在同一 DPI-unaware 上下文比较窗口和显示器矩形。另检查显式坐标、启动中退出和有限 `TryFindWindow` 超时，无需修改游戏或档案文件。

本次验证：Windows 上 Release 测试 128 项通过；实际游戏以 `-win -fullscreen -xres 1280 -yres 720` 启动，恢复响应后进行定位，在 DPI-unaware 坐标系下窗口为 1024×576，最终左上角为 `(512, 288)`，与显示器边界算出的居中位置一致。未验证多显示器；本机单屏，显示器选择另记 [issue #21](https://github.com/mrbbbaixue/Ra3.Tools/issues/21)。

发布工具依赖升级到 `Microsoft.SourceLink.GitHub` 10.0.402，避开其传递依赖 `Microsoft.Build.Tasks.Git` 的 [GHSA-23fw-v26w-5fgq](https://github.com/advisories/GHSA-23fw-v26w-5fgq) 安全公告，否则现有警告视为错误设置会阻止还原。发布继续沿用 `.github/workflows/publish.yml`：Windows/.NET 10 上还原、构建、测试和打包，再通过 `v*` 标签（或手动触发工作流）执行 NuGet 受信任发布。

## 示例

### 读取安装目录

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

### 重建注册表

```csharp
using Ra3.Tools.Installation;

var registry = new Ra3Registry();
registry.Rebuild(@"D:\Games\Red Alert 3", "YOUR-CD-KEY");
registry.TrySetLanguage(@"D:\Games\Red Alert 3", "english");
```

### 编辑 SkuDef

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

### 切换 4GB Patch

```csharp
using Ra3.Tools.Patching;

var patchService = new Ra3LargeAddressPatchService();
var gamePath = @"D:\Games\Red Alert 3\Data\ra3_1.12.game";

if (!patchService.IsEnabled(gamePath))
{
    patchService.Enable(gamePath);
}
```

### 获取推荐分辨率

```csharp
using Ra3.Tools.Graphics;

var displayService = new WindowsDisplayService();
var supported = displayService.GetSupportedDisplayModes();
var current = displayService.GetCurrentDisplayMode();
var recommended = Ra3DisplayAdvisor.GetRecommendedDisplayMode(supported, current);
```

### 安装地图包

```csharp
using Ra3.Tools.IO;
using Ra3.Tools.Installation;

var installed = ZipMapInstaller.InstallMapPackage(@"D:\Downloads\map_pack.zip", Ra3KnownPaths.MapsDirectoryPath);
```

### 生成 Steam AppID

```csharp
using Ra3.Tools.Installation;

if (Ra3InstallationValidator.NeedsSteamAppId(@"D:\Games\Red Alert 3"))
{
    Ra3InstallationValidator.GenerateSteamAppId(@"D:\Games\Red Alert 3");
}
```

### 解析在线档案

```csharp
using Ra3.Tools.Profiles;

var login = GameSpyLoginDocument.Load(@"%appdata%\Red Alert 3\Profiles\PlayerOne\GameSpyLogin.ini");
foreach (var account in login.Accounts)
{
    Console.WriteLine($"{account.Email} / {account.Nickname} / {account.DecryptPassword()}");
}

var identity = Ra3ProfileData.Load(@"%appdata%\Red Alert 3\Profiles\PlayerOne\ProfileData.ini");
Console.WriteLine(identity.PublicKey); // 游戏 ID（PPK）
```

## 命名空间

- `Ra3.Tools.Installation`
- `Ra3.Tools.Configuration`
- `Ra3.Tools.Graphics`
- `Ra3.Tools.IO`
- `Ra3.Tools.Patching`
- `Ra3.Tools.Profiles`
- `Ra3.Tools.Versioning`

## 说明

- 目标框架：`net10.0-windows7.0`
- 这个包只包含本地 RA3 工具能力，不包含更新器、下载器、联机平台逻辑和任何产品专属模组逻辑。

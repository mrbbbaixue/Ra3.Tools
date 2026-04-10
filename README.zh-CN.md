![Ra3 Tools 100px](https://user-images.githubusercontent.com/17956756/148715792-5db21b5b-5a81-4b29-b88a-478e863cb8b1.png)

# Ra3.Tools

[English](README.md) | [简体中文](README.zh-CN.md)

一个仅面向 Windows 的 RA3 SDK，提供本地安装管理、启动辅助、SkuDef 读写、4GB Patch、显示推荐和文件工具。

## 功能

- 检测 RA3 安装目录并定位常用路径
- 读取、修复注册表并切换语言
- 支持普通启动、游戏大厅启动和窗口化启动
- 读取和写回 SkuDef，同时保留未知原始行
- 为 `ra3_1.12.game` 启用或关闭 4GB Patch
- 枚举显示模式并给出适合 RA3 的推荐分辨率
- 提供共享文件读取和 CRC32C 校验工具

## 安装

```powershell
dotnet add package Ra3.Tools
```

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

## 命名空间

- `Ra3.Tools.Installation`
- `Ra3.Tools.Configuration`
- `Ra3.Tools.Graphics`
- `Ra3.Tools.IO`
- `Ra3.Tools.Patching`
- `Ra3.Tools.Versioning`

## 说明

- 目标框架：`net8.0-windows7.0`
- 这个包只包含本地 RA3 工具能力，不包含更新器、下载器、联机平台逻辑和任何产品专属模组逻辑。

# Ra3.Tools v2.x RA3 功能并入计划（API 文档草案 · 修订版）

> 状态：**计划（草案）**，尚未实施。
> 范围修订：只并入 **RA3 直接相关**的功能；上一版 A 组（通用工具层）、C 组（下载/更新基础设施）、D 组（产品专属）**全部不做**。
> 目标：并入项尽量自包含、**零新增 NuGet 依赖**，可被 Client / CoronaLauncher 直接替换使用。

---

## 1. 并入清单（精简版）

| # | 功能 | 来源 | 现状 | 建议 | 优先级 | 新依赖 |
|---|---|---|---|---|---|---|
| 1 | RA3 启动参数改写引擎（RewriteArguments / PredictActualRA3Path / TestIsPathRa3Like / FormatWithQuotes） | Corona `Models/GameLauncher.cs` | `Ra3CommandLine` 已有 SplitArgs、可执行文件提取 | 扩展 `Ra3CommandLine`（Corona 前缀判断、skudef 生成不并入） | P0 | 无 |
| 2 | `ea_debug_v1` 崩溃管道监听（GameCrashed 事件） | Corona `BackgroundTasks/EaDebugIpc.cs` | 无 | 新增 `Ra3.Tools.Diagnostics.EaDebugIpc` | P0 | 无（BCL 管道） |
| 3 | 崩溃日志关键字分析（资源包损坏 / 地图崩溃提取地图名） | Client `App.xaml.cs`（Application_Startup 日志段） | 无 | 新增 `Ra3.Tools.Diagnostics.CrashLogAnalyzer` | P1 | 无 |
| 4 | 地图 zip 包安装（防 Zip Slip + 递归找 .map + 平铺安装到 `%appdata%\Red Alert 3\Maps`） | Client `MainWindow.xaml.cs`（DownloadStarting 分支） | `Ra3KnownPaths.MapsDirectoryPath` 已有 | 新增 `Ra3.Tools.IO.ZipMapInstaller` | P1 | 无（System.IO.Compression） |
| 5 | Steam AppID 生成（写 `steam_appid.txt` = "17480"） | Corona `Utils/Ra3Utils.cs` | 已有 `NeedsSteamAppId` 检测 | 扩展 `Ra3InstallationValidator` 补写入 | P1 | 无 |
| 6 | 命名管道服务器（给 RA3 游戏进程传设置，Message 模式 + WorldSid ACL） | Client `Models/ClientSettingToGame.cs` | 无 | 新增 `Ra3.Tools.Interop.Windows.PipeServer`（消息泛化，去掉 App 静态依赖） | P1 | 无（NamedPipeServerStreamAcl） |
| 7 | RA3 游戏内命名事件监听（EventWaitHandleAcl + 窗口置前） | Client `Models/Notification.cs` | 无 | 新增 `NamedEventWatcher`（弹窗改回调，事件名由调用方给） | P2 | 无（EventWaitHandleAcl） |
| 8 | RA3 进程/窗口查找（按类名 `41DAF790-...` + 1.12 版本校验，**不含注入执行**） | Client `Shared/Injector.cs` | 无 | 新增 `Ra3.Tools.Processes.Ra3ProcessFinder`（注入动作留给产品） | 待决策 | user32 P/Invoke |

可选（确认需要再做）：RA3 世界建造器（WorldBuilder）启动，Corona `Models/WorldBuilderLauncher.cs` → 扩展 `Ra3Launcher`，P3。

明确不做：ProcessWrapper / ProgressUtils / ACL / 自启 / 加密 / JSON 配置等通用层；aria2 / zstd / RSS / 更新作业等下载更新层；Injector 注入执行、NativeDll、Corona 模组逻辑、AppHostPatcher（GPLv3）。

---

## 2. API 设计草案

### 2.1 `Ra3.Tools.Installation`（扩展）

```csharp
// #1 启动参数改写（补充到 Ra3CommandLine）
public static partial class Ra3CommandLine
{
    /// <summary>改写 RA3 启动参数（引号重排、-config/-modconfig 定位、vanilla 判定）。</summary>
    public static string RewriteArguments(string input, bool convertVanillaRa3, Ra3Version version);
    /// <summary>从参数串预测实际 RA3 可执行文件路径。</summary>
    public static string? PredictActualRA3Path(string input);
    public static bool TestIsPathRa3Like(string path);
    public static string FormatWithQuotes(string path); // 含空格路径加引号
}

// #5 Steam AppID（补充到 Ra3InstallationValidator）
public static partial class Ra3InstallationValidator
{
    public static void GenerateSteamAppId(string path); // 写 steam_appid.txt = "17480"
}

// 可选：世界建造器
public sealed partial class Ra3Launcher
{
    public void LaunchWorldBuilder(Ra3Installation installation, string worldBuilderPath, string arguments = "");
}
```

### 2.2 `Ra3.Tools.Diagnostics`（新增）

```csharp
// #2 RA3 引擎崩溃管道
public sealed class EaDebugIpc : IDisposable
{
    public event EventHandler? GameCrashed;
    public Task RunAsync(CancellationToken cancellationToken = default); // 监听 ea_debug_v1
}

// #3 崩溃日志分析（纯字符串解析）
public static class CrashLogAnalyzer
{
    public static CrashLogReport Analyze(string logText);
}
public sealed record CrashLogReport(
    bool ResourceCorrupted,        // Ra3BattleNetSpecialContentLoadingBegin/End 不配对
    string? MapNameAtCrash,        // Ra3BattleNetGameOpenMapBegin/End 提取
    bool CrashDetected);
```

### 2.3 `Ra3.Tools.IO`（扩展）

```csharp
// #4 地图包安装
public static class ZipMapInstaller
{
    /// <summary>解压地图 zip 到 maps 根目录（防 Zip Slip，递归找 .map 平铺安装），返回安装的 .map 列表。</summary>
    public static IReadOnlyList<string> InstallMapPackage(string zipPath, string mapsRootPath);
}
```

### 2.4 `Ra3.Tools.Interop.Windows`（扩展）

```csharp
// #6 命名管道服务器（Message 模式、WorldSid ACL）
public sealed class PipeServer : IDisposable
{
    public PipeServer(string pipeName, Func<string, string?> messageHandler,
        int maxInstances = 10, CancellationToken cancellationToken = default);
    public Task RunAsync(CancellationToken cancellationToken = default);
}

// #7 RA3 游戏内命名事件监听
public sealed class NamedEventWatcher : IDisposable
{
    public NamedEventWatcher(IEnumerable<string> eventNames, Action<string> onSignal, bool bringGameWindowToFront);
    public Task RunAsync(CancellationToken cancellationToken = default);
}
```

### 2.5 `Ra3.Tools.Processes`（新增，待决策）

```csharp
// #8 RA3 窗口/进程查找（不含注入）
public static class Ra3ProcessFinder
{
    public static IntPtr[] FindRa3WindowsByEnumWindows();       // 类名 41DAF790-... 前缀 7
    public static bool IsRa3Process(Process process);            // 进程名含 1.12 校验
    public static IntPtr GetRa3MainWindow();                     // 置前用
}
```

---

## 3. 实施计划

| 阶段 | 版本 | 内容 | 验收 |
|---|---|---|---|
| 一期 | 2.1.0 | #1–#6 | 新增单测；Corona 的 GameLauncher 参数改写、EaDebugIpc 可切到本库实现 |
| 二期 | 2.2.0 | #7、可选 WB、决策 #8 | Client 的 Notification / 地图安装 / 设置管道切到本库实现 |
| 决策 | — | #8 是否并入（见第 5 节） | — |

## 4. 测试

- 每个新 API 配 `Ra3.Tools.Test` 单测（沿用现有 TestDouble 风格）。
- `ZipMapInstaller`：构造含 `../` 路径的 zip 验证 Zip Slip 拒绝；递归嵌套目录找 .map。
- `Ra3CommandLine.RewriteArguments`：对照 Corona 现有测试用例（引号、-config、vanilla 转换）。
- `PipeServer`/`NamedEventWatcher`：集成测试用真实管道 + 临时命名事件，超时兜底。

## 5. 决策点

1. **#8 Injector 窗口查找是否并入**：只抽查找/校验（无注入执行、无位数问题）→ 建议并入；注入执行留给产品。
2. **多目标框架**：#1–#7 均为纯 BCL 逻辑，不依赖 net10 特有 API；若需兼容 Client（net8.0）可改 `net8.0-windows7.0;net10.0-windows7.0` 多目标——建议等 2.0 重构方案定调后一并决策。

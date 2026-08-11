# Ra3.Tools 2.0 重构与优化方案

> 状态：**方案（草案）**。
> 范围：仅针对当前 2.0 分支已发布代码（`net10.0-windows7.0`，AsmResolver.PE 6.0）的重构与优化，不含新功能并入（见 API-PLAN.zh-CN.md）。
> 优先级：P0 = 正确性，P1 = 性能/API 一致性，P2 = 工程质量。

---

## 1. P0 · 正确性（潜在缺陷，建议优先处理）

### 1.1 `Ra3Version` 语义确认与修复（Versioning/Ra3Version.cs）

现有问题：
- `TryParse` 要求小版本 **恰好 3 位**（`parts[1].Length != 3` 即失败）→ `Ra3Version.Parse("1.12")` 会抛异常。游戏实际版本是 `1.12`（2 位），`Ra3VersionPattern` 默认也匹配不了 `ra3_1.12.game` 这类文件名。
- `NormalizeMinor` 把字母小版本统一替换为 `0` → `1.12A` 与 `1.12B` 比较结果**相等**，无法表达渠道/内测版本先后（Corona 侧 `corona_X.YYY` 的字母结尾是有序的）。

建议：
- 确认设计意图后二选一：① 允许小版本 2–3 位（长度 ≥ 2 且 ≤ 3，尾部可带一个字母）；② 若刻意只支持 3 位，则补文档与测试明确行为。
- 字母比较改为按字典序参与排序（A < B < … < Z），或保留"忽略字母"语义但写成显式常量并补测试。
- 补充测试：`1.12` / `1.12A` / `1.12B` / `1.120` 解析与排序用例。

### 1.2 `Ra3ProfileStore.ParseDirectoryString` 越界风险（Installation/Ra3ProfileStore.cs）

- `value[index + 1]`、`value[index + 2]` 在 `_` 位于串尾时越界抛 `IndexOutOfRangeException`（畸形 directory.ini 直接崩溃）。
- 建议：下标检查，非法输入跳过该字符或返回 null/空列表（与库内其他服务的容错风格一致）。
- `GetCurrentProfile` 中 `value.Length > 15 ? value[15..] : null` 的 **15 是魔数**（`"ProfileName: "` 前缀长度）→ 提取命名常量。

### 1.3 `FileSystemService.GetDirectorySizeBytes` 可跟随重解析点（IO/FileSystemService.cs）

- 递归 `EnumerateDirectories` 不检查 `ReparsePoint`，遇到 junction 环（如 OneDrive/同步目录）会死循环或重复统计。
- 建议：跳过 `FileAttributes.ReparsePoint` 目录（与 `ClearDirectory` 现有防重解析点逻辑对齐）。

### 1.4 `Ra3OptionsIniService` 匹配过宽（Installation/Ra3OptionsIniService.cs）

- `line.StartsWith("FirewallPortOverride", StringComparison.Ordinal)` 大小写敏感且会误删 `FirewallPortOverrideXxx=...` 之类键。
- 建议：键名精确匹配（`Split('=')[0].Trim()` 比较）+ 大小写不敏感；保留 section 头行与注释行。

---

## 2. P1 · 性能与 API 一致性

### 2.1 `Ra3LargeAddressPatchService` 全文件读入内存（Patching/）

- `IsEnabled`/`SetState` 用 `SharedFile.ReadAllBytesAsync(...).GetAwaiter().GetResult()` 把整个 `ra3_1.12.game`（约 30 MB）读入内存并复制一份再写回。
- 建议：
  - 用 `PEFile.FromFile`（AsmResolver 6 支持从流加载）或直接定位 COFF 头偏移（PE 头固定偏移 0x3C 指向 COFF 头），**只读写头部约 0x100 字节**；
  - 消除 `.GetAwaiter().GetResult()` 反模式：同步方法就同步读（`FileStream`），或整体提供 `async` API（与 `ChecksumService` 风格对齐）。
- 保留现有"备份 + 差异校验 + 覆盖写回"流程（与 Corona 侧 `Game4GBPatchInstaller` 行为一致，设计合理）。

### 2.2 `Ra3Launcher` 两个启动入口能力不齐（Installation/Ra3Launcher.cs）

- `Launch()` 支持 `LaunchMode`/自定义参数，但不支持 skudef；`LaunchWithConfig()` 支持 skudef 但不支持模式。
- 建议：`Ra3LaunchOptions` 增加 `SkudefPath` 字段，`Launch()` 统一两条路径（内部仍走 `UnelevatedProcessLauncher`），`LaunchWithConfig` 保留为兼容重载。

### 2.3 `ChecksumService` 补同步重载（IO/ChecksumService.cs）

- 目前只有 `ComputeCrc32cAsync`；`Ra3LargeAddressPatchService` 等同步场景被迫阻塞等待。
- 建议：抽公共核心，提供 `ComputeCrc32c(path, mode)` 同步重载 + async 包装。

### 2.4 小型性能项

- `Ra3Installation` 路径属性每次 `Path.Combine` → 构造时惰性缓存（属性调用频繁）。
- `Ra3Version.NormalizeMinor` 的正则替换 → 手写字符映射（微优化，低优先）。
- `WindowsDisplayService.GetSupportedDisplayModes` 每次全量枚举 → 可选静态缓存（注意分辨率热插拔场景，低优先）。

---

## 3. P2 · 工程质量

### 3.1 公共 API 缺少 XML 文档（全库）

- csproj 已开 `GenerateDocumentationFile`，但代码零 XML 注释，且 `NoWarn 1591` 把缺失警告全部压掉 → NuGet 包 XML 文档基本是空壳。
- 建议：去掉 `1591` NoWarn，为全部公共 API 补 XML doc（README 示例 + 本方案 1.1 的语义说明正是文档重点）；不补的不允许编译通过，形成强制约束。

### 3.2 SourceLink 配置未生效

- csproj 已设 `PublishRepositoryUrl`/`EmbedUntrackedSources`，但**缺少 `Microsoft.SourceLink.GitHub` 包**，SourceLink 实际不会生成。
- 建议：加 `Microsoft.SourceLink.GitHub`（PrivateAssets=all）。

### 3.3 无 CI

- 仓库无 `.github/workflows` → 加 GitHub Actions：PR/推送触发 `dotnet build`（warnings-as-errors 已开）+ `dotnet test` + `dotnet pack`，可选的 NuGet 发布 job。

### 3.4 版本号硬编码

- `Version`/`AssemblyVersion`/`FileVersion` 2.0.0 硬编码在 csproj → 建议 `Directory.Build.props` 统一（参考 CoronaLauncher 的 `Directory.Build.props` 惯例），tag 发布时单点改版本。

### 3.5 目标框架决策

- 2.0 已升 `net10.0-windows7.0`，但 Client 仍是 net8.0，无法引用新包。
- 建议：`net8.0-windows7.0;net10.0-windows7.0` 多目标（本库 API 无 net10 特有依赖，代价小）；若 Client 计划升级 net10 则可维持单目标——需产品侧定夺。

### 3.6 边界声明与 README

- README 已声明"不包含更新器、下载器、联机平台逻辑和任何产品专属模组逻辑"——本次只并 RA3 层功能，**声明无需大改**；补充新功能示例（参数改写、崩溃管道、地图安装）。

---

## 4. 测试补强（P1）

| 现状 | 缺口 | 建议 |
|---|---|---|
| 已有：SkuDef、GpuPreference、DisplayAdvisor、Checksum、FileSystem、CommandLine、Validator、Language、Launcher、OptionsIni、ProfileStore、Registry、4GB Patch、Ra3Version 测试 | `Ra3Version` 边界（1.12 / 字母序） | 补 1.1 所列用例 |
| — | `Ra3ProfileStore` 畸形输入 | 补 1.2 用例 |
| — | `Ra3OptionsIniService` 大小写/前缀 | 补 1.4 用例 |
| — | `WindowsDisplayService` / `VideoControllerService` / `Ra3KnownPaths`（依赖真实系统） | 维持现状即可（TestDouble 成本高），标注为手工验证项 |

---

## 5. 建议路线图

1. **2.0.1（小修）**：P0 全部（Ra3Version 语义、越界保护、重解析点、OptionsIni 匹配）+ 对应测试。
2. **2.1.0（重构 + RA3 功能并入一期）**：P1（4GB 补丁 IO、Launcher 统一、Checksum 同步重载）+ API-PLAN #1–#6。
3. **2.2.0（工程化）**：P2（XML doc、SourceLink、CI、Directory.Build.props、多目标）+ API-PLAN 二期。

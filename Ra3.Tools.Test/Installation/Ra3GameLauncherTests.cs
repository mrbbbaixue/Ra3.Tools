using Ra3.Tools.Installation;
using Ra3.Tools.Versioning;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3GameLauncherTests
{
    [Fact]
    public void BuildStartInfo_LaunchesGameDataExecutableWithConfig()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);

        var startInfo = Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), new Ra3LaunchOptions(string.Empty, EnsureUnelevated: false));

        Assert.Equal(Path.Combine(installPath, "Data", "RA3_1.12.game"), startInfo.FileName);
        Assert.Equal($"-config \"{Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef")}\"", startInfo.Arguments);
        Assert.Equal(installPath, startInfo.WorkingDirectory);
        Assert.True(startInfo.UseShellExecute);
    }

    [Fact]
    public void BuildStartInfo_AssemblesRa3BarStyleArguments()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);
        var modSkudefPath = Path.Combine(installPath, "my mod", "example.skudef");
        Directory.CreateDirectory(Path.GetDirectoryName(modSkudefPath)!);
        File.WriteAllText(modSkudefPath, "mod-game 1.12");
        var replayPath = Path.Combine(installPath, "my replay.ra3replay");

        var options = new Ra3LaunchOptions(
            "-win -xres 1920 -yres 1080",
            RunVersion: Ra3Version.Parse("1.12"),
            ModConfigPath: modSkudefPath,
            ReplayPath: replayPath,
            EnsureUnelevated: false);

        var startInfo = Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options);

        Assert.Equal(Path.Combine(installPath, "Data", "RA3_1.12.game"), startInfo.FileName);
        Assert.Equal(
            $"-win -xres 1920 -yres 1080 -modConfig \"{modSkudefPath}\" -replayGame \"{replayPath}\" -config \"{Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef")}\"",
            startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_ModGameOverridesRunVersion()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);
        File.WriteAllText(Path.Combine(installPath, "RA3_chinese_t_1.13.SkuDef"), "mod-game 1.13\nset-exe Data\\RA3_1.12.game");
        var modSkudefPath = Path.Combine(installPath, "my mod.skudef");
        File.WriteAllText(modSkudefPath, "mod-game 1.13");

        var options = new Ra3LaunchOptions(string.Empty, RunVersion: Ra3Version.Parse("1.12"), ModConfigPath: modSkudefPath, EnsureUnelevated: false);

        var startInfo = Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options);

        Assert.Contains($"-config \"{Path.Combine(installPath, "RA3_chinese_t_1.13.SkuDef")}\"", startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_ExplicitSkudefPathSkipsResolution()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);
        var explicitSkudefPath = Path.Combine(installPath, "explicit.skudef");
        File.WriteAllText(explicitSkudefPath, "mod-game 1.12\nset-exe Data\\ra3_1.12.game");

        var options = new Ra3LaunchOptions(string.Empty, SkudefPath: explicitSkudefPath, EnsureUnelevated: false);

        var startInfo = Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options);

        Assert.Contains($"-config \"{explicitSkudefPath}\"", startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_ModSkudefWithoutSetExeFallsBackToRa3Exe()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);
        // MOD 的 SkuDef 一般没有 set-exe（游戏数据 SkuDef 独有）。
        var modSkudefPath = Path.Combine(installPath, "my mod.skudef");
        File.WriteAllText(modSkudefPath, "add-big Data\\Mod.big");

        var options = new Ra3LaunchOptions(string.Empty, SkudefPath: modSkudefPath, EnsureUnelevated: false);

        var startInfo = Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options);

        Assert.Equal(Path.Combine(installPath, "RA3.exe"), startInfo.FileName);
        Assert.Equal($"-config \"{modSkudefPath}\"", startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_AppendsLaunchModeArgument()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);

        var options = new Ra3LaunchOptions(string.Empty, Ra3LaunchMode.Windowed, EnsureUnelevated: false);

        var startInfo = Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options);

        Assert.StartsWith("-win ", startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_ThrowsWhenExplicitSkudefMissing()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);

        var options = new Ra3LaunchOptions(string.Empty, SkudefPath: Path.Combine(installPath, "missing.skudef"), EnsureUnelevated: false);

        Assert.Throws<FileNotFoundException>(() => Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options));
    }

    [Fact]
    public void BuildStartInfo_ThrowsWhenRunVersionSkudefMissing()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);

        var options = new Ra3LaunchOptions(string.Empty, RunVersion: Ra3Version.Parse("1.13"), EnsureUnelevated: false);

        Assert.Throws<FileNotFoundException>(() => Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options));
    }

    [Fact]
    public void BuildStartInfo_ThrowsWhenModConfigMissing()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);

        var options = new Ra3LaunchOptions(string.Empty, ModConfigPath: Path.Combine(installPath, "missing.skudef"), EnsureUnelevated: false);

        Assert.Throws<FileNotFoundException>(() => Ra3GameLauncher.BuildStartInfo(new Ra3Installation(installPath), options));
    }

    [Fact]
    public void Launch_WithMissingSkudef_ThrowsWithoutStartingProcess()
    {
        var installPath = CreateInstallDirectory(includeLauncher: false);

        var options = new Ra3LaunchOptions(string.Empty, SkudefPath: Path.Combine(installPath, "missing.skudef"), EnsureUnelevated: false);

        Assert.Throws<FileNotFoundException>(() => new Ra3GameLauncher().Launch(new Ra3Installation(installPath), options));
    }

    private static string CreateInstallDirectory(bool includeLauncher)
    {
        var installPath = Path.Combine(Path.GetTempPath(), "ra3_game_launcher", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installPath);
        Directory.CreateDirectory(Path.Combine(installPath, "Data"));
        File.WriteAllText(Path.Combine(installPath, "RA3.exe"), string.Empty);
        File.WriteAllText(Path.Combine(installPath, "Data", "ra3_1.12.game"), string.Empty);
        File.WriteAllText(
            Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef"),
            "mod-game 1.12\nset-exe Data\\RA3_1.12.game");
        if (includeLauncher)
        {
            Directory.CreateDirectory(Path.Combine(installPath, "Launcher"));
            File.WriteAllText(Path.Combine(installPath, "Launcher", "chinese_t.csf"), string.Empty);
        }

        return installPath;
    }
}

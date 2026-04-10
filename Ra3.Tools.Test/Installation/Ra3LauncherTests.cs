using Ra3.Tools.Installation;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3LauncherTests
{
    [Fact]
    public void BuildStartInfo_AppendsGameBrowserArgument()
    {
        var options = new Ra3LaunchOptions("-runver 1.12", Ra3LaunchMode.GameBrowser, EnsureUnelevated: false);

        var startInfo = Ra3Launcher.BuildStartInfo("RA3.exe", "D:\\Games\\RA3", options);

        Assert.Equal("-ui -runver 1.12", startInfo.Arguments);
        Assert.Equal("D:\\Games\\RA3", startInfo.WorkingDirectory);
    }

    [Fact]
    public void BuildStartInfo_AppendsWindowedArgument()
    {
        var options = new Ra3LaunchOptions(string.Empty, Ra3LaunchMode.Windowed, EnsureUnelevated: true);

        var startInfo = Ra3Launcher.BuildStartInfo("RA3.exe", "D:\\Games\\RA3", options);

        Assert.Equal("-win", startInfo.Arguments);
    }
}

using Ra3.Tools.Installation;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3CommandLineTests
{
    [Fact]
    public void TryExtractGameExecutablePath_HandlesSpecialSpaces()
    {
        var root = Path.Combine(Path.GetTempPath(), "Red  Alert 3.game", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Data"));
        File.WriteAllText(Path.Combine(root, "RA3.exe"), string.Empty);
        var gamePath = Path.Combine(root, "Data", "ra3_1.12.game");
        File.WriteAllText(gamePath, "<<< DID YOU REMEMBER TO BUILD DATA??? >>>");

        try
        {
            var commandLine = $"{gamePath} -modconfig \"D:\\Mods\\example.skudef\"";
            Assert.Equal(Path.GetFullPath(gamePath), Ra3CommandLine.TryExtractGameExecutablePath(commandLine));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void IsRa3LikeExecutable_ReturnsTrueForRa3Exe()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Data"));
        var executablePath = Path.Combine(root, "RA3.exe");
        File.WriteAllText(executablePath, string.Empty);
        File.WriteAllText(Path.Combine(root, "Data", "ra3_1.12.game"), "<<< DID YOU REMEMBER TO BUILD DATA??? >>>");

        try
        {
            Assert.True(Ra3CommandLine.IsRa3LikeExecutable(executablePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

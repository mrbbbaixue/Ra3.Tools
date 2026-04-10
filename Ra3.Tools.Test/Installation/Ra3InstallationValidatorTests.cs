using Ra3.Tools.Installation;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3InstallationValidatorTests
{
    [Fact]
    public void IsInstallDirectory_ReturnsTrueForValidInstall()
    {
        var installPath = CreateInstallDirectory(includeLauncher: true);

        try
        {
            Assert.True(Ra3InstallationValidator.IsInstallDirectory(installPath));
            Assert.True(Ra3InstallationValidator.HasRequiredGameFiles(installPath));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void NeedsSteamAppId_ReturnsTrueWhenMissing()
    {
        var installPath = CreateInstallDirectory(includeLauncher: true, baseFolderName: "steam_ra3");

        try
        {
            Assert.True(Ra3InstallationValidator.NeedsSteamAppId(installPath));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void NeedsPatchedParFile_ReturnsTrueForMismatchedPar()
    {
        var installPath = CreateInstallDirectory(includeLauncher: true);
        File.WriteAllBytes(Path.Combine(installPath, "Data", "ra3_1.12.par"), [0x01, 0x02, 0x03]);

        try
        {
            Assert.True(Ra3InstallationValidator.NeedsPatchedParFile(installPath));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    internal static string CreateInstallDirectory(bool includeLauncher, string baseFolderName = "ra3_install")
    {
        var installPath = Path.Combine(Path.GetTempPath(), baseFolderName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installPath);
        Directory.CreateDirectory(Path.Combine(installPath, "Data"));
        File.WriteAllText(Path.Combine(installPath, "RA3.exe"), string.Empty);
        File.WriteAllText(Path.Combine(installPath, "Data", "ra3_1.12.game"), "<<< DID YOU REMEMBER TO BUILD DATA??? >>>");
        File.WriteAllText(Path.Combine(installPath, "RA3_english_1.12.SkuDef"), "mod-game 1.12");
        Directory.CreateDirectory(Path.Combine(installPath, "Support"));
        File.WriteAllText(Path.Combine(installPath, "Support", "readme.txt"), "readme");
        if (includeLauncher)
        {
            Directory.CreateDirectory(Path.Combine(installPath, "Launcher"));
            File.WriteAllText(Path.Combine(installPath, "Launcher", "english.csf"), string.Empty);
        }

        return installPath;
    }
}

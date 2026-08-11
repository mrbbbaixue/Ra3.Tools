using Ra3.Tools.Installation;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3LanguageCatalogTests
{
    [Fact]
    public void GetValidAndInvalidLanguages_ReturnExpectedResults()
    {
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);
        File.WriteAllText(Path.Combine(installPath, "Launcher", "chinese_t.csf"), string.Empty);
        File.WriteAllText(Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef"), "mod-game 1.12");
        File.WriteAllText(Path.Combine(installPath, "Launcher", "broken.csf"), string.Empty);

        try
        {
            var service = new Ra3LanguageCatalog();

            var valid = service.GetValidLanguages(installPath);
            var invalid = service.GetInvalidLanguageFiles(installPath);

            Assert.Contains("english", valid);
            Assert.Contains("chinese_t", valid);
            Assert.Contains(Path.Combine(installPath, "Launcher", "broken.csf"), invalid);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }
}

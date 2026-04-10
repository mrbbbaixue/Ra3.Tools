using Ra3.Tools.Installation;
using Ra3.Tools.Internal;
using Ra3.Tools.Test.TestDoubles;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3OptionsIniServiceTests
{
    [Fact]
    public void RemoveFirewallPortOverrideFromAllProfiles_RemovesDangerousSetting()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var originalProvider = SpecialFolderProvider.Current;
        SpecialFolderProvider.Current = new TestSpecialFolderProvider
        {
            MyDocumentsPath = root,
            ApplicationDataPath = root
        };
        var installation = new Ra3Installation(root);
        var profilePath = Path.Combine(installation.ProfilesDirectoryPath, "PlayerOne");
        Directory.CreateDirectory(profilePath);
        File.WriteAllText(Path.Combine(profilePath, "Options.ini"), "Resolution = 1920 1080\r\nFirewallPortOverride = 1234");

        var service = new Ra3OptionsIniService();

        try
        {
            Assert.Equal(1, service.RemoveFirewallPortOverrideFromAllProfiles(installation));
            Assert.DoesNotContain("FirewallPortOverride", File.ReadAllText(Path.Combine(profilePath, "Options.ini")));
        }
        finally
        {
            SpecialFolderProvider.Current = originalProvider;
            Directory.Delete(root, recursive: true);
        }
    }
}

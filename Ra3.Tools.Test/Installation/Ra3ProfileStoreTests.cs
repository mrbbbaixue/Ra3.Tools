using System.Text;
using Ra3.Tools.Installation;
using Ra3.Tools.Internal;
using Ra3.Tools.Test.TestDoubles;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3ProfileStoreTests
{
    [Fact]
    public void GetProfilesAndCurrentProfile_ReadFromDirectoryIni()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var originalProvider = SpecialFolderProvider.Current;
        SpecialFolderProvider.Current = new TestSpecialFolderProvider
        {
            MyDocumentsPath = root,
            ApplicationDataPath = root
        };
        var installation = new Ra3Installation(root);
        Directory.CreateDirectory(installation.ProfilesDirectoryPath);
        Directory.CreateDirectory(Path.Combine(installation.ProfilesDirectoryPath, "PlayerOne"));
        Directory.CreateDirectory(Path.Combine(installation.ProfilesDirectoryPath, "Other"));

        File.WriteAllLines(Path.Combine(installation.ProfilesDirectoryPath, "directory.ini"),
        [
            EncodeDirectoryString("PlayerOne"),
            EncodeDirectoryString(new string('x', 15) + "PlayerOne")
        ]);

        var store = new Ra3ProfileStore();

        try
        {
            Assert.Contains("PlayerOne", store.GetProfiles(installation));
            Assert.Equal("PlayerOne", store.GetCurrentProfile(installation));
        }
        finally
        {
            SpecialFolderProvider.Current = originalProvider;
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DeleteAllSkirmishIni_RemovesFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var originalProvider = SpecialFolderProvider.Current;
        SpecialFolderProvider.Current = new TestSpecialFolderProvider
        {
            MyDocumentsPath = root,
            ApplicationDataPath = root
        };
        var installation = new Ra3Installation(root);
        Directory.CreateDirectory(Path.Combine(installation.ProfilesDirectoryPath, "PlayerOne"));
        File.WriteAllLines(Path.Combine(installation.ProfilesDirectoryPath, "directory.ini"),
        [
            EncodeDirectoryString("PlayerOne"),
            EncodeDirectoryString(new string('x', 15) + "PlayerOne")
        ]);
        File.WriteAllText(Path.Combine(installation.ProfilesDirectoryPath, "PlayerOne", "Skirmish.ini"), "data");

        var store = new Ra3ProfileStore();

        try
        {
            store.DeleteAllSkirmishIni(installation);
            Assert.False(File.Exists(Path.Combine(installation.ProfilesDirectoryPath, "PlayerOne", "Skirmish.ini")));
        }
        finally
        {
            SpecialFolderProvider.Current = originalProvider;
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetProfiles_WithMalformedDirectoryIni_DoesNotThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var originalProvider = SpecialFolderProvider.Current;
        SpecialFolderProvider.Current = new TestSpecialFolderProvider
        {
            MyDocumentsPath = root,
            ApplicationDataPath = root
        };
        var installation = new Ra3Installation(root);
        Directory.CreateDirectory(installation.ProfilesDirectoryPath);
        File.WriteAllLines(Path.Combine(installation.ProfilesDirectoryPath, "directory.ini"), ["PlayerOne_"]);

        var store = new Ra3ProfileStore();

        try
        {
            var profiles = store.GetProfiles(installation);
            Assert.NotNull(profiles);
            Assert.Null(store.GetCurrentProfile(installation));
        }
        finally
        {
            SpecialFolderProvider.Current = originalProvider;
            Directory.Delete(root, recursive: true);
        }
    }

    private static string EncodeDirectoryString(string value)
    {
        var builder = new StringBuilder();
        foreach (var current in Encoding.Unicode.GetBytes(value))
        {
            if (current is >= 32 and <= 126 && current != (byte)'_')
            {
                builder.Append((char)current);
            }
            else
            {
                builder.Append('_').Append(current.ToString("X2"));
            }
        }

        return builder.ToString();
    }
}

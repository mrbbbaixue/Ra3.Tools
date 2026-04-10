namespace Ra3.Tools.Installation;

public sealed class Ra3ProfileStore
{
    public IReadOnlyList<string> GetProfiles(Ra3Installation installation)
    {
        var directoryPath = Path.Combine(installation.ProfilesDirectoryPath, "directory.ini");
        if (!File.Exists(directoryPath))
        {
            return Array.Empty<string>();
        }

        var lines = File.ReadAllLines(directoryPath);
        if (lines.Length == 0)
        {
            return Array.Empty<string>();
        }

        var original = ParseDirectoryString(lines[0]);
        return Directory.Exists(installation.ProfilesDirectoryPath)
            ? Directory.GetDirectories(installation.ProfilesDirectoryPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(profile => profile != null && original.Contains(profile, StringComparison.Ordinal))
                .Cast<string>()
                .ToArray()
            : Array.Empty<string>();
    }

    public string? GetCurrentProfile(Ra3Installation installation)
    {
        var directoryPath = Path.Combine(installation.ProfilesDirectoryPath, "directory.ini");
        if (!File.Exists(directoryPath))
        {
            return null;
        }

        var lines = File.ReadAllLines(directoryPath);
        if (lines.Length < 2)
        {
            return null;
        }

        var value = ParseDirectoryString(lines[1]);
        return value.Length > 15 ? value[15..] : null;
    }

    public void DeleteSkirmishIni(Ra3Installation installation, string profile)
    {
        var skirmishPath = Path.Combine(installation.ProfilesDirectoryPath, profile, "Skirmish.ini");
        if (File.Exists(skirmishPath))
        {
            File.Delete(skirmishPath);
        }
    }

    public void DeleteAllSkirmishIni(Ra3Installation installation)
    {
        foreach (var profile in GetProfiles(installation))
        {
            DeleteSkirmishIni(installation, profile);
        }
    }

    private static string ParseDirectoryString(string value)
    {
        var bytes = new List<byte>();
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (current != '_')
            {
                bytes.Add(Convert.ToByte(current));
                continue;
            }

            var hex = new[] { value[index + 1], value[index + 2] };
            var parsed = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            bytes.Add(Convert.ToByte(parsed));
            index += 2;
        }

        return System.Text.Encoding.Unicode.GetString(bytes.ToArray());
    }
}

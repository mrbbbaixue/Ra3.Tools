namespace Ra3.Tools.Installation;

public sealed class Ra3OptionsIniService
{
    public bool RemoveFirewallPortOverride(string optionsIniPath)
    {
        if (!File.Exists(optionsIniPath))
        {
            return false;
        }

        var lines = File.ReadAllLines(optionsIniPath);
        var filtered = lines.Where(line => !line.StartsWith("FirewallPortOverride", StringComparison.Ordinal)).ToArray();
        if (filtered.Length == lines.Length)
        {
            return false;
        }

        File.WriteAllLines(optionsIniPath, filtered);
        return true;
    }

    public int RemoveFirewallPortOverrideFromAllProfiles(Ra3Installation installation)
    {
        if (!Directory.Exists(installation.ProfilesDirectoryPath))
        {
            return 0;
        }

        var fixedFiles = 0;
        foreach (var profilePath in Directory.GetDirectories(installation.ProfilesDirectoryPath))
        {
            if (RemoveFirewallPortOverride(Path.Combine(profilePath, "Options.ini")))
            {
                fixedFiles++;
            }
        }

        return fixedFiles;
    }
}

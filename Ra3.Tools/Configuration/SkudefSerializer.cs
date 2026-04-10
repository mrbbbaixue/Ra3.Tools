namespace Ra3.Tools.Configuration;

public static class SkudefSerializer
{
    public static SkudefDocument Load(string path)
    {
        var entries = new List<SkudefEntry>();
        var header = "mod-game 1.12";
        if (!File.Exists(path))
        {
            return new SkudefDocument(header, entries);
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                entries.Add(new RawEntry(line));
                continue;
            }

            if (line.StartsWith("mod-game ", StringComparison.OrdinalIgnoreCase))
            {
                header = line.Trim();
                continue;
            }

            if (line.StartsWith("set-search-path ", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(new SetSearchPathEntry(line["set-search-path ".Length..].Trim()));
                continue;
            }

            if (line.StartsWith("add-config ", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(new AddConfigEntry(line["add-config ".Length..].Trim().Trim('"')));
                continue;
            }

            if (line.StartsWith("add-big ", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(new AddBigEntry(line["add-big ".Length..].Trim().Trim('"')));
                continue;
            }

            entries.Add(new RawEntry(line));
        }

        return new SkudefDocument(header, entries);
    }

    public static void Save(string path, SkudefDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var lines = new List<string> { document.Header };
        foreach (var entry in document.Entries)
        {
            lines.Add(entry switch
            {
                SetSearchPathEntry searchPath => $"set-search-path {searchPath.Command}",
                AddConfigEntry config => $"add-config {config.Path}",
                AddBigEntry big => $"add-big {big.Path}",
                RawEntry raw => raw.Text,
                _ => throw new NotSupportedException($"Unsupported skudef entry type: {entry.GetType().FullName}")
            });
        }

        File.WriteAllLines(path, lines);
    }
}

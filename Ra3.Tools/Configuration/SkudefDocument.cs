namespace Ra3.Tools.Configuration;

public sealed record SkudefDocument(string Header, IReadOnlyList<SkudefEntry> Entries);

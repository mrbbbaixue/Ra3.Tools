namespace Ra3.Tools.Configuration;

public abstract record SkudefEntry;

public sealed record SetSearchPathEntry(string Command) : SkudefEntry;

public sealed record AddConfigEntry(string Path) : SkudefEntry;

public sealed record AddBigEntry(string Path) : SkudefEntry;

public sealed record RawEntry(string Text) : SkudefEntry;

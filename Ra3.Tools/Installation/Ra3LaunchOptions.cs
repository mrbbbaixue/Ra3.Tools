namespace Ra3.Tools.Installation;

public sealed record Ra3LaunchOptions(string Arguments, Ra3LaunchMode LaunchMode = Ra3LaunchMode.Normal, bool EnsureUnelevated = true)
{
    public static Ra3LaunchOptions Default { get; } = new(string.Empty);
}

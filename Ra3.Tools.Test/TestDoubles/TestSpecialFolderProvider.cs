using Ra3.Tools.Internal;

namespace Ra3.Tools.Test.TestDoubles;

internal sealed class TestSpecialFolderProvider : ISpecialFolderProvider
{
    public string MyDocumentsPath { get; init; } = string.Empty;

    public string ApplicationDataPath { get; init; } = string.Empty;

    public string GetMyDocumentsPath()
    {
        return MyDocumentsPath;
    }

    public string GetApplicationDataPath()
    {
        return ApplicationDataPath;
    }
}

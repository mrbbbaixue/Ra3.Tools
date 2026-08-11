namespace Ra3.Tools.Internal;

internal interface ISpecialFolderProvider
{
    string GetMyDocumentsPath();

    string GetApplicationDataPath();
}

internal sealed class DefaultSpecialFolderProvider : ISpecialFolderProvider
{
    public string GetMyDocumentsPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public string GetApplicationDataPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}

internal static class SpecialFolderProvider
{
    internal static ISpecialFolderProvider Current { get; set; } = new DefaultSpecialFolderProvider();
}

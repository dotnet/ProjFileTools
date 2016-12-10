namespace PackageFeedManager
{
    public interface IPackageInfo
    {
        string Id { get; }

        string IconUrl { get; }

        string Description { get; }

        string Authors { get; }

        string LicenseUrl { get; }

        string ProjectUrl { get; }

        string Version { get; }

        SourceKind SourceKind { get; }
    }
}

namespace PackageFeedManager
{
    public interface IPackageQueryConfiguration
    {
        string CompatibiltyTarget { get; }

        bool IncludePreRelease { get; }

        int MaxResults { get; }
    }
}

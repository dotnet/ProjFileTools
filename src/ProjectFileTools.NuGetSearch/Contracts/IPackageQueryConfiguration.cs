namespace ProjectFileTools.NuGetSearch.Contracts
{
    public interface IPackageQueryConfiguration
    {
        string CompatibilityTarget { get; }

        bool IncludePreRelease { get; }

        int MaxResults { get; }

        PackageType PackageType { get; }
    }
}

using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.NuGetSearch.Feeds
{
    public class PackageQueryConfiguration : IPackageQueryConfiguration
    {
        public PackageQueryConfiguration(string targetFrameworkMoniker, bool includePreRelease = true, int maxResults = 100)
        {
            CompatibiltyTarget = targetFrameworkMoniker;
            IncludePreRelease = includePreRelease;
            MaxResults = maxResults;
        }

        public string CompatibiltyTarget { get; }

        public bool IncludePreRelease { get; }

        public int MaxResults { get; }

        public override int GetHashCode()
        {
            return (CompatibiltyTarget?.GetHashCode() ?? 0) ^ IncludePreRelease.GetHashCode() ^ MaxResults.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PackageQueryConfiguration cfg = obj as PackageQueryConfiguration;
            return cfg != null
                && string.Equals(CompatibiltyTarget, cfg.CompatibiltyTarget, System.StringComparison.Ordinal)
                && IncludePreRelease == cfg.IncludePreRelease
                && MaxResults == cfg.MaxResults;
        }
    }
}
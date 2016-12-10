using PackageFeedManager;

namespace PackageFeedManagerTests
{
    public class PackageQueryConfiguration : IPackageQueryConfiguration
    {
        public PackageQueryConfiguration(string tfm, int maxResults = 100, bool includePreRelease= false)
        {
            CompatibiltyTarget = tfm;
            MaxResults = maxResults;
            IncludePreRelease = includePreRelease;
        }

        public string CompatibiltyTarget { get; }

        public bool IncludePreRelease { get; }

        public int MaxResults { get; }
    }
}

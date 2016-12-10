using System;

namespace PackageFeedManager
{
    internal class PackageQueryConfiguration : IPackageQueryConfiguration
    {
        public PackageQueryConfiguration(string targetFrameworkMoniker, bool includePreRelease = false, int maxResults = 100)
        {
            CompatibiltyTarget = targetFrameworkMoniker;
            IncludePreRelease = includePreRelease;
            MaxResults = maxResults;
        }

        public string CompatibiltyTarget { get; }

        public bool IncludePreRelease { get; }

        public int MaxResults { get; }
    }
}
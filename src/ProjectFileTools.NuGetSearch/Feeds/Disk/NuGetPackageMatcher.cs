using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.IO;

namespace ProjectFileTools.NuGetSearch.Feeds.Disk
{
    internal static class NuGetPackageMatcher
    {
        public static bool IsMatch(string dir, IPackageInfo info, IPackageQueryConfiguration queryConfiguration, IFileSystem fileSystem)
        {
            if (!queryConfiguration.IncludePreRelease)
            {
                SemanticVersion ver = SemanticVersion.Parse(info.Version);

                if(!string.IsNullOrEmpty(ver?.PrereleaseVersion))
                {
                    return false;
                }
            }

			return true;
        }
    }
}

using System.IO;
using System.Linq;
using NuGet.Frameworks;

namespace PackageFeedManager
{
    internal static class NuGetPackageMatcher
    {
        public static bool IsMatch(NuGetFramework targetFramework, string dir, IPackageInfo info, IPackageQueryConfiguration queryConfiguration, IFileSystem fileSystem)
        {
            if (!queryConfiguration.IncludePreRelease)
            {
                SemanticVersion ver = SemanticVersion.Parse(info.Version);

                if(!string.IsNullOrEmpty(ver?.PrereleaseVersion))
                {
                    return false;
                }
            }

            string libPath = Path.Combine(dir, "lib");
            string buildPath = Path.Combine(dir, "build");
            
            return (fileSystem.DirectoryExists(libPath) && fileSystem.EnumerateDirectories(libPath, "*", SearchOption.TopDirectoryOnly).Any(x => DefaultCompatibilityProvider.Instance.IsCompatible(targetFramework, NuGetFramework.ParseFolder(fileSystem.GetDirectoryName(x)))))
                || (fileSystem.DirectoryExists(buildPath) && fileSystem.EnumerateDirectories(buildPath, "*", SearchOption.TopDirectoryOnly).Any(x => DefaultCompatibilityProvider.Instance.IsCompatible(targetFramework, NuGetFramework.ParseFolder(fileSystem.GetDirectoryName(x)))));
        }
    }
}

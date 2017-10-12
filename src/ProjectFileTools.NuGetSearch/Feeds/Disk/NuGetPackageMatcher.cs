using System.IO;
using System.Linq;
using NuGet.Frameworks;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.IO;

namespace ProjectFileTools.NuGetSearch.Feeds.Disk
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

			if (fileSystem.DirectoryExists (libPath)) {
				foreach (var fxDir in fileSystem.EnumerateDirectories (libPath, "*", SearchOption.TopDirectoryOnly)) {
					var fxName = NuGetFramework.ParseFolder (Path.GetFileName (fxDir));
					if (DefaultCompatibilityProvider.Instance.IsCompatible (targetFramework, fxName)) {
						return true;
					}
				}
			}

			if (fileSystem.DirectoryExists (buildPath)) {
				foreach (var fxDir in fileSystem.EnumerateDirectories (buildPath, "*", SearchOption.TopDirectoryOnly)) {
					var fxName = NuGetFramework.ParseFolder (Path.GetFileName (fxDir));
					if (DefaultCompatibilityProvider.Instance.IsCompatible (targetFramework, fxName)) {
						return true;
					}
				}
			}

			return false;
        }
    }
}

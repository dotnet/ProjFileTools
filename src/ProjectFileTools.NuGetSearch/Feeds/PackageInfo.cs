using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.NuGetSearch.Feeds
{

    public class PackageInfo : IPackageInfo
    {
        public PackageInfo(string id, string version, string authors, string description, string licenseUrl, string projectUrl, string iconUrl, FeedKind sourceKind)
        {
            Id = id;
            Version = version;
            Authors = authors;
            Description = description;
            LicenseUrl = licenseUrl;
            ProjectUrl = projectUrl;
            SourceKind = sourceKind;
            IconUrl = iconUrl;
        }

        public string Authors { get; }

        public string Description { get; }

        public string Version { get; }

        public string LicenseUrl { get; }

        public string ProjectUrl { get; }

        public string IconUrl { get; }

        public string Id { get; }

        public FeedKind SourceKind { get; }
    }
}

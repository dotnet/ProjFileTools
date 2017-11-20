using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.NuGetSearch.Feeds
{

    public class PackageInfo : IPackageInfo
    {
        public PackageInfo(string id, string version, string title, string authors, string summary, string description, string licenseUrl, string projectUrl, string iconUrl, string tags, FeedKind sourceKind)
        {
            Id = id;
            Version = version;
            Title = title;
            Authors = authors;
            Description = description;
            LicenseUrl = licenseUrl;
            ProjectUrl = projectUrl;
            SourceKind = sourceKind;
            IconUrl = iconUrl;
            Tags = tags;
        }

        public string Id { get; }

        public string Version { get; }

        public string Title { get; }

        public string Authors { get; }

        public string Summary { get; }

        public string Description { get; }

        public string LicenseUrl { get; }

        public string ProjectUrl { get; }

        public string IconUrl { get; }

        public string Tags { get; }

        public FeedKind SourceKind { get; }
    }
}

using System.Xml.Linq;
using ProjectFileTools.NuGetSearch.Contracts;

namespace ProjectFileTools.NuGetSearch.Feeds
{

    internal class NuSpecReader
    {
        internal static IPackageInfo Read(string nuspec, FeedKind kind)
        {
            XDocument doc = XDocument.Load(nuspec);
            XNamespace ns = doc.Root.GetDefaultNamespace();
            XElement package = doc.Root;
            XElement metadata = package?.Element(XName.Get("metadata", ns.NamespaceName));
            XElement id = metadata?.Element(XName.Get("id", ns.NamespaceName));
            XElement version = metadata?.Element(XName.Get("version", ns.NamespaceName));
            XElement title = metadata?.Element(XName.Get("title", ns.NamespaceName));
            XElement authors = metadata?.Element(XName.Get("authors", ns.NamespaceName));
            XElement summary = metadata?.Element (XName.Get ("summary", ns.NamespaceName));
            XElement description = metadata?.Element(XName.Get("description", ns.NamespaceName));
            XElement licenseUrl = metadata?.Element(XName.Get("licenseUrl", ns.NamespaceName));
            XElement projectUrl = metadata?.Element (XName.Get ("projectUrl", ns.NamespaceName));
            XElement iconUrl = metadata?.Element (XName.Get ("iconUrl", ns.NamespaceName));
            XElement tags = metadata?.Element(XName.Get("tags", ns.NamespaceName));

            if (id != null)
            {
                return new PackageInfo(id.Value, version?.Value, title?.Value, authors?.Value, summary?.Value, description?.Value, licenseUrl?.Value, projectUrl?.Value, iconUrl?.Value, tags?.Value, kind);
            }

            return null;
        }
    }
}

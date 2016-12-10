using System.Xml.Linq;

namespace PackageFeedManager
{

    internal class NuSpecReader
    {
        internal static IPackageInfo Read(string nuspec, SourceKind kind)
        {
            XDocument doc = XDocument.Load(nuspec);
            XNamespace ns = doc.Root.GetDefaultNamespace();
            XElement package = doc.Root;
            XElement metadata = package?.Element(XName.Get("metadata", ns.NamespaceName));
            XElement id = metadata?.Element(XName.Get("id", ns.NamespaceName));
            XElement title = metadata?.Element(XName.Get("title", ns.NamespaceName));
            XElement version = metadata?.Element(XName.Get("version", ns.NamespaceName));
            XElement authors = metadata?.Element(XName.Get("authors", ns.NamespaceName));
            XElement description = metadata?.Element(XName.Get("description", ns.NamespaceName));
            XElement licenseUrl = metadata?.Element(XName.Get("licenseUrl", ns.NamespaceName));
            XElement projectUrl = metadata?.Element(XName.Get("projectUrl", ns.NamespaceName));

            if (id != null)
            {
                return new PackageInfo(id.Value, title?.Value ?? id.Value, version?.Value, authors?.Value, description?.Value, licenseUrl?.Value, projectUrl?.Value, kind);
            }

            return null;
        }
    }
}

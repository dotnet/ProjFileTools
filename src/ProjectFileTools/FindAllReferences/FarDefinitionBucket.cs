using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using ProjectFileTools.MSBuild;

/// <summary>
/// An <see cref="IEntryBucket2"/> displaying a symbol definition bucket.
/// A bucket displays in the table as a grouping row, under which entries in the same bucket are displaied.
/// </summary>
public class FarDefinitionBucket : DefinitionBucket
{
    public const string BucketIdentifier = "Far_DefinitionBucket";

    internal readonly Definition Location;

    public FarDefinitionBucket(Definition location)
        : base(location.Type, BucketIdentifier, BucketIdentifier)
    {
        Location = location;
    }

    public override bool TryGetValue(string key, out object content)
    {

        switch (key)
        {
            case StandardTableKeyNames.Text:
                content = this.Name;
                return true;
            case StandardTableKeyNames.DocumentName:
                content = Location.File;
                return true;
            case StandardTableKeyNames.Line:
                content = 0;
                return true;
            case StandardTableKeyNames.Column:
                content = 0;
                return true;
            case StandardTableKeyNames2.DefinitionIcon:
                // icon image of this bucket (displayed in front of the bucket content)
                content = KnownMonikers.TypeDefinition;
                return true;
            case StandardTableKeyNames2.TextInlines:
                // content of the bucket displayed as a rich text
                List<Inline> inlines = new List<Inline>();
                inlines.Add(new Run(this.Name) { FontWeight = FontWeights.Bold });
                content = inlines;
                return true;
            case StandardTableKeyNames.HelpKeyword:
                content = "FindAllReferences";
                return true;
            case StandardTableKeyNames.HelpLink:
                content = "https://www.visualstudio.com/";
                return true;
            case "IPersistentSpan":
                break;
            default:
                Debug.Fail($"Unknown bucket key: {key}");
                break;
        }

        content = null;
        return false;
    }
}
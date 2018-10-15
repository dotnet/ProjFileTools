using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using ProjectFileTools.MSBuild;

namespace FarTestProvider
{
    public class FarDataSnapshot : WpfTableEntriesSnapshotBase
    {
        private static int versionGenerator = -1;
        private readonly int _versionNumber;
        private readonly List<Definition> _definitions;
        private IList<FarDefinitionBucket> _buckets;

        public FarDataSnapshot(List<Definition> definitions)
        {
            _definitions = definitions;

            _versionNumber = Interlocked.Increment(ref versionGenerator);

            _buckets = new FarDefinitionBucket[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                _buckets[i] = new FarDefinitionBucket(definitions[i]);
            }
        }

        public override int Count { get { return _buckets.Count; } }

        public override int VersionNumber { get { return _versionNumber; } }

        /// <summary>
        /// Find All References table calls this method for information
        /// </summary>
        public override bool TryGetValue(int index, string keyName, out object content)
        {
            Debug.Assert(index >= 0 && index < _buckets.Count);

            FarDefinitionBucket bucket = _buckets[index % _buckets.Count];

            switch (keyName)
            {
                case StandardTableKeyNames.ProjectName:
                    content = bucket.Location.Project;
                    return true;
                case StandardTableKeyNames.DocumentName:
                    content = bucket.Location.File;
                    return true;
                case StandardTableKeyNames.Line:
                    // line #
                    content = bucket.Location.Line.GetValueOrDefault() - 1;
                    return true;
                case StandardTableKeyNames.Column:
                    // col #
                    content = bucket.Location.Col.GetValueOrDefault() - 1;
                    return true;
                case StandardTableKeyNames.Text:
                    content = bucket.Location.Type;
                    return true;
                case StandardTableKeyNames.FullText:
                case StandardTableKeyNames2.TextInlines:
                    {
                        List<Inline> inlines = new List<Inline>
                        {
                            new Run(bucket.Location.Text) { FontWeight = FontWeights.Bold }
                        };

                        content = inlines;
                        return true;
                    }
                case StandardTableKeyNames2.Definition:
                    {
                        // queries which definition bucket this entry belongs to
                        content = bucket;
                        return true;
                    }
                case StandardTableKeyNames.HelpKeyword:
                    content = "FindAllReferences";
                    return true;
                case StandardTableKeyNames.HelpLink:
                    content = "https://www.visualstudio.com/";
                    return true;
                case StandardTableKeyNames.HasVerticalContent:
                case StandardTableKeyNames.DetailsExpander:
                case StandardTableColumnDefinitions2.LineText:
                case StandardTableKeyNames2.ProjectNames:
                case "IPersistentSpan":
                    break;
                default:
                    Debug.Fail($"Unknown column key: {keyName}");
                    break;
            }

            content = null;
            return false;
        }
    }
}

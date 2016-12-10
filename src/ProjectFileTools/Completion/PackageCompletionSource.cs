using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using PackageFeedManager;

namespace ProjectFileTools
{

    internal class PackageCompletionSource : ICompletionSource
    {
        private readonly ICompletionBroker _completionBroker;
        private IPackageFeedSearchJob<Tuple<string, FeedKind>> _nameSearchJob;
        private readonly IPackageSearchManager _searchManager;
        private readonly ITextBuffer _textBuffer;
        private IPackageFeedSearchJob<Tuple<string, FeedKind>> _versionSearchJob;
        private ICompletionSession _currentSession;
        private PackageCompletionSet _currentCompletionSet;
        private int _pos;
        private readonly IClassifier _classifier;

        public PackageCompletionSource(ITextBuffer textBuffer, ICompletionBroker completionBroker, IClassifierAggregatorService classifier, IPackageSearchManager searchManager)
        {
            _classifier = classifier.GetClassifier(textBuffer);
            _searchManager = searchManager;
            _textBuffer = textBuffer;
            _completionBroker = completionBroker;
        }

        public static bool IsInRangeForPackageCompletion(ITextSnapshot snapshot, int pos, out Span span, out string packageName, out string packageVersion, out string completionType)
        {
            if(pos < 1)
            {
                span = default(Span);
                packageName = null;
                packageVersion = null;
                completionType = null;
                return false;
            }

            string documentText = snapshot.GetText();
            int start = documentText.LastIndexOf('<', pos);
            int end = documentText.IndexOf('>', pos);
            int startQuote = documentText.LastIndexOf('"', pos - 1);
            int endQuote = documentText.IndexOf('"', pos);

            if (start < 0 || end < 0 || startQuote < 0 || endQuote < 0 || startQuote < start || endQuote > end || endQuote <= startQuote)
            {
                span = default(Span);
                packageName = null;
                packageVersion = null;
                completionType = null;
                return false;
            }

            string fragmentText = documentText.Substring(start, end - start + 1);
            string attributeText = documentText.Substring(startQuote + 1, endQuote - startQuote - 1);

            XElement element;
            try
            {
                element = XElement.Parse(fragmentText);
            }
            catch
            {
                span = default(Span);
                packageName = null;
                packageVersion = null;
                completionType = null;
                return false;
            }

            if (element.Name != "PackageReference")
            {
                span = default(Span);
                packageName = null;
                packageVersion = null;
                completionType = null;
                return false;
            }

            XAttribute name = element.Attribute(XName.Get("Include"));
            XAttribute version = element.Attribute(XName.Get("Version"));
            string nameValue = name?.Value;
            string versionValue = version?.Value;

            if (nameValue == attributeText)
            {
                //Package name completion
                completionType = "Name";
                packageName = nameValue;
                packageVersion = versionValue;
                span = new Span(startQuote + 1, endQuote - startQuote - 1);
                return true;
            }

            if (versionValue == attributeText)
            {
                //Package version completion
                completionType = "Version";
                packageName = nameValue;
                packageVersion = versionValue;
                span = new Span(startQuote + 1, endQuote - startQuote - 1);
                return true;
            }

            completionType = null;
            packageName = null;
            packageVersion = null;
            span = default(Span);
            return false;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            _currentSession = session;
            ITextSnapshot snapshot = _textBuffer.CurrentSnapshot;
            ITrackingPoint point = session.GetTriggerPoint(_textBuffer);
            int pos = point.GetPosition(snapshot);

            if(_pos == pos && _currentSession != null)
            {
                completionSets.Add(_currentCompletionSet);
                return;
            }

            if (_classifier.GetClassificationSpans(new SnapshotSpan(snapshot, new Span(pos, 1))).Any(x => (x.ClassificationType.Classification?.IndexOf("comment", StringComparison.OrdinalIgnoreCase) ?? -1) > -1))
            {
                return;
            }

            _pos = pos;
            if (!IsInRangeForPackageCompletion(snapshot, pos, out Span span, out string name, out string version, out string completionType))
            {
                _nameSearchJob?.Cancel();
                _versionSearchJob?.Cancel();
                _nameSearchJob = null;
                _versionSearchJob = null;
                return;
            }

            bool showLoading = false;
            switch (completionType)
            {
                case "Name":
                    if (_nameSearchJob != null)
                    {
                        _nameSearchJob.Cancel();
                        _nameSearchJob.Updated -= UpdateCompletions;
                    }
                    _versionSearchJob?.Cancel();
                    _versionSearchJob = null;
                    _nameSearchJob = _searchManager.SearchPackageNames(name, "netcoreapp1.0");
                    _nameSearchJob.Updated += UpdateCompletions;
                    showLoading = true;
                    break;
                case "Version":
                    if (_versionSearchJob != null)
                    {
                        _versionSearchJob.Cancel();
                        _versionSearchJob.Updated -= UpdateCompletions;
                    }
                    _nameSearchJob?.Cancel();
                    _nameSearchJob = null;
                    _versionSearchJob = _searchManager.SearchPackageVersions(name, "netcoreapp1.0");
                    _versionSearchJob.Updated += UpdateCompletions;
                    showLoading = true;
                    break;
            }

            if (showLoading)
            {
                _currentCompletionSet = new PackageCompletionSet("PackageCompletion", "Package Completion", _textBuffer.CurrentSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive));
                _currentCompletionSet.AccessibleCompletions.Add(new Completion("Loading..."));

                completionSets.Add(_currentCompletionSet);
            }
        }

        private void ProduceNameCompletionSet()
        {
            List<Completion> completions = new List<Completion>();
            Dictionary<string, FeedKind> packageLookup = new Dictionary<string, FeedKind>();

            foreach(Tuple<string, FeedKind> info in _nameSearchJob.Results)
            {
                if(!packageLookup.TryGetValue(info.Item1, out FeedKind existingInfo) || info.Item2 == FeedKind.Local)
                {
                    packageLookup[info.Item1] = info.Item2;
                }
            }

            foreach (KeyValuePair<string, FeedKind> entry in packageLookup)
            {
                ImageMoniker moniker = KnownMonikers.NuGet;

                switch (entry.Value)
                {
                    case FeedKind.Local:
                        moniker = KnownMonikers.FolderClosed;
                        break;
                        //TODO: Add different icons for MyGet/network/etc
                }

                completions.Add(new Completion4(entry.Key, entry.Key, entry.Key, moniker, entry.Key));
            }

            _currentCompletionSet.AccessibleCompletions.Clear();

            if (_nameSearchJob.RemainingFeeds.Count > 0)
            {
                _currentCompletionSet.AccessibleCompletions.Add(new Completion($"Loading ({_nameSearchJob.RemainingFeeds.Count} remaining)..."));
            }
            else if (completions.Count == 0)
            {
                _currentCompletionSet.AccessibleCompletions.Add(new Completion("(No Results)"));
            }

            _currentCompletionSet.AccessibleCompletions.AddRange(completions);
        }

        private void ProduceVersionCompletionSet()
        {
            List<Completion> completions = new List<Completion>();
            Dictionary<string, FeedKind> iconMap = new Dictionary<string, FeedKind>();

            foreach (Tuple<string, FeedKind> info in _versionSearchJob.Results)
            {
                if (!iconMap.TryGetValue(info.Item1, out FeedKind existing) || existing != FeedKind.Local)
                {
                    iconMap[info.Item1] = info.Item2;
                }
            }

            foreach(KeyValuePair<string, FeedKind> entry in iconMap.OrderByDescending(x => SemanticVersion.Parse(x.Key)))
            {
                ImageMoniker moniker = KnownMonikers.NuGet;

                switch (entry.Value)
                {
                    case FeedKind.Local:
                        moniker = KnownMonikers.FolderClosed;
                        break;
                        //TODO: Add different icons for MyGet/network/etc
                }

                completions.Add(new Completion3(entry.Key, entry.Key, null, moniker, entry.Key));
            }

            _currentCompletionSet.AccessibleCompletions.Clear();

            if (_versionSearchJob.RemainingFeeds.Count > 0)
            {
                _currentCompletionSet.AccessibleCompletions.Add(new Completion($"Loading ({_versionSearchJob.RemainingFeeds.Count} remaining)..."));
            }
            else if (completions.Count == 0)
            {
                _currentCompletionSet.AccessibleCompletions.Add(new Completion("(No Results)"));
            }

            _currentCompletionSet.AccessibleCompletions.AddRange(completions);
        }

        private void UpdateCompletions(object sender, EventArgs e)
        {
            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                string displayText = _currentCompletionSet.SelectionStatus.Completion.DisplayText;

                if (!_currentSession.IsDismissed)
                {
                    if (_nameSearchJob != null)
                    {
                        ProduceNameCompletionSet();
                    }
                    else if (_versionSearchJob != null)
                    {
                        ProduceVersionCompletionSet();
                    }

                    _currentSession.Filter();

                    foreach(Completion completion in _currentSession.SelectedCompletionSet.Completions)
                    {
                        if(completion.DisplayText == displayText)
                        {
                            _currentCompletionSet.SelectionStatus = new CompletionSelectionStatus(completion, true, true);
                            break;
                        }
                    }

                    //_currentSession.Dismiss();
                    //_currentSession = _completionBroker.TriggerCompletion(_currentSession.TextView);
                }
            });
        }

        public void Dispose()
        {
        }
    }
}

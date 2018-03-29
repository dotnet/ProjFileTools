using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using ProjectFileTools.NuGetSearch;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.Completion
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
        private bool _isSelfTrigger;

        public PackageCompletionSource(ITextBuffer textBuffer, ICompletionBroker completionBroker, IClassifierAggregatorService classifier, IPackageSearchManager searchManager)
        {
            _classifier = classifier.GetClassifier(textBuffer);
            _searchManager = searchManager;
            _textBuffer = textBuffer;
            _completionBroker = completionBroker;
            textBuffer.Properties.AddProperty(typeof(PackageCompletionSource), this);
        }

        private static bool TryGetExtents(ITextSnapshot snapshot, int pos, 
            out string documentText, out int start, out int end, out int startQuote, out int endQuote,
            out int realEnd, out bool isHealed, out string healedXml)
        {
            documentText = snapshot.GetText();
            start = pos < documentText.Length ? documentText.LastIndexOf('<', pos) : documentText.LastIndexOf('<');
            end = pos < documentText.Length ? documentText.IndexOf('>', pos) : -1;
            startQuote = documentText.LastIndexOf('"', pos - 1);
            endQuote = pos < documentText.Length ? documentText.IndexOf('"', pos) : -1;
            realEnd = end;

            if (startQuote > -1 && startQuote < start || end > -1 && endQuote > end)
            {
                healedXml = null;
                isHealed = false;
                return false;
            }

            string fragmentText = null;
            //If we managed to find a close...
            if (end > start)
            {
                int nextStart = start < documentText.Length - 1 ? documentText.IndexOf('<', start + 1) : -1;

                //If we found another start before the close...
                //      <PackageReference Include="
                //  </ItemGroup>
                if (nextStart > -1 && nextStart < end)
                {
                    //Heal
                    fragmentText = documentText.Substring(start, nextStart - start).Trim();
                    realEnd = fragmentText.Length + start;

                    switch (fragmentText[fragmentText.Length - 1])
                    {
                        case '\"':
                            if (endQuote > nextStart || endQuote < 0)
                            {
                                endQuote = fragmentText.Length + start;
                                fragmentText += "\"";
                            }
                            break;
                        case '>':
                            healedXml = null;
                            isHealed = false;
                            return false;
                        default:
                            //If there's a start quote in play, we're just looking at an unclosed attribute value
                            if (startQuote > start)
                            {
                                endQuote = fragmentText.Length + start;
                                fragmentText += "\"";
                            }
                            else
                            {
                                healedXml = null;
                                isHealed = false;
                                return false;
                            }
                            break;
                    }

                    end = fragmentText.Length + 2 + start;
                    fragmentText += " />";
                    isHealed = true;
                }
                //We didn't find that, so we're "good"... we might have a non-self closed tag though
                else
                {
                    isHealed = false;

                    //Unless of course the closing quote was after the end...
                    if (endQuote < 0 || endQuote > end)
                    {
                        fragmentText = documentText.Substring(start, end - start);

                        if (fragmentText.EndsWith("/"))
                        {
                            fragmentText = fragmentText.Substring(0, fragmentText.Length - 1);
                        }

                        fragmentText = fragmentText.TrimEnd() + "\" />";

                        endQuote = fragmentText.Length - 4 + start;
                        end = fragmentText.Length - 1 + start;
                    }
                    else
                    {
                        fragmentText = documentText.Substring(start, end - start + 1);
                    }
                }
            }
            //If we didn't find a close, if we found an end quote, that's good enough
            else if (endQuote > start)
            {
                realEnd = endQuote;
                fragmentText = documentText.Substring(start, endQuote - start + 1) + " />";
                end = fragmentText.Length - 1 + start;
                isHealed = true;
            }
            //If we didn't find an end quote even, if we found an open quote, that might be good enough
            else if (startQuote > start)
            {
                //If we find a close before the cursor that's after the start quote, we're outside of the element
                if (documentText.LastIndexOf('>', pos - 1) > startQuote)
                {
                    healedXml = null;
                    isHealed = false;
                    return false;
                }

                //Otherwise, we're presumably just after the start quote (and maybe some text) at the end of the document
                //  we already know there's no closing quote or end, see if there's another start we can run up to
                int nextStart = pos < documentText.Length ? documentText.IndexOf('<', pos) : -1;

                //If there isn't, run off to the end of the document
                if (nextStart < 0)
                {
                    fragmentText = documentText.Substring(start, documentText.Length - start).TrimEnd();
                }
                else
                {
                    fragmentText = documentText.Substring(start, nextStart - start + 1);
                    fragmentText = fragmentText.Trim();
                }

                realEnd = start + fragmentText.Length - 1;
                fragmentText += "\" />";
                endQuote = fragmentText.Length - 4 + start;
                end = fragmentText.Length - 1 + start;
                isHealed = true;
            }
            //All we've got to go on is the start, that's not good enough
            else
            {
                healedXml = null;
                isHealed = false;
                return false;
            }

            healedXml = fragmentText;
            return true;
        }

        public static bool TryHealOrAdvanceAttributeSelection(ITextSnapshot snapshot, ref int pos, out Span targetSpan, out string newText, out bool isHealingRequired)
        {
            if (pos < 1)
            {
                isHealingRequired = false;
                newText = null;
                targetSpan = default(Span);
                return false;
            }

            if (!TryGetExtents(snapshot, pos, out string documentText, out int start, out int end, out int startQuote, out int endQuote, out int realEnd, out isHealingRequired, out string healedXml))
            {
                newText = null;
                targetSpan = default(Span);
                return false;
            }

            XElement element;
            try
            {
                element = XElement.Parse(healedXml);
            }
            catch
            {
                newText = null;
                targetSpan = default(Span);
                return false;
            }

            if (element.Name != "PackageReference" && element.Name != "DotNetCliToolReference")
            {
                newText = null;
                targetSpan = default(Span);
                return false;
            }

            XAttribute name = element.Attribute(XName.Get("Include"));
            XAttribute version = element.Attribute(XName.Get("Version"));
            newText = healedXml;

            if (version == null)
            {
                isHealingRequired = true;
                element.SetAttributeValue(XName.Get("Version"), "");
                newText = element.ToString();
                string versionString = "Version=\"";
                pos = newText.IndexOf(versionString) + versionString.Length + start;
                targetSpan = new Span(start, realEnd - start + 1);
                return true;
            }

            int versionIndex = newText.IndexOf("Version");
            int quoteIndex = newText.IndexOf('"', versionIndex);
            int proposedPos = start + quoteIndex + 1;
            bool move = pos < proposedPos;
            pos = proposedPos;
            targetSpan = new Span(start, realEnd - start + 1);
            return move;
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

            if (!TryGetExtents(snapshot, pos, out string documentText, out int start, out int end, out int startQuote, out int endQuote, out int realEnd, out bool isHealingRequired, out string healedXml))
            {
                span = default(Span);
                packageName = null;
                packageVersion = null;
                completionType = null;
                return false;
            }

            XElement element;
            try
            {
                element = XElement.Parse(healedXml);
            }
            catch
            {
                span = default(Span);
                packageName = null;
                packageVersion = null;
                completionType = null;
                return false;
            }

            if (element.Name != "PackageReference" && element.Name != "DotNetCliToolReference")
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
            string attributeText = healedXml.Substring(startQuote + 1 - start, endQuote - startQuote - 1);

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

            if (pos < snapshot.Length && _classifier.GetClassificationSpans(new SnapshotSpan(snapshot, new Span(pos, 1))).Any(x => (x.ClassificationType.Classification?.IndexOf("comment", StringComparison.OrdinalIgnoreCase) ?? -1) > -1))
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

            if (_isSelfTrigger)
            {
                _isSelfTrigger = false;
                if(_currentCompletionSet != null)
                {
                    completionSets.Add(_currentCompletionSet);
                }

                return;
            }

            string text = snapshot.GetText();
            int targetFrameworkElementStartIndex = text.IndexOf("<TargetFramework>", StringComparison.OrdinalIgnoreCase);
            int targetFrameworksElementStartIndex = text.IndexOf("<TargetFrameworks>", StringComparison.OrdinalIgnoreCase);
            string tfm = "netcoreapp1.0";

            if (targetFrameworksElementStartIndex > -1)
            {
                int closeTfms = text.IndexOf("</TargetFrameworks>", targetFrameworksElementStartIndex);
                int realStart = targetFrameworksElementStartIndex + "<TargetFrameworks>".Length;
                string allTfms = text.Substring(realStart, closeTfms - realStart);
                tfm = allTfms.Split(';')[0];
            }
            else if (targetFrameworkElementStartIndex > -1)
            {
                int closeTfm = text.IndexOf("</TargetFramework>", targetFrameworkElementStartIndex);
                int realStart = targetFrameworkElementStartIndex + "<TargetFramework>".Length;
                tfm = text.Substring(realStart, closeTfm - realStart);
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
                    _nameSearchJob = _searchManager.SearchPackageNames(name, tfm);
                    _nameSearchJob.Updated += UpdateCompletions;
                    showLoading = _nameSearchJob.RemainingFeeds.Count > 0;
                    break;
                case "Version":
                    if (_versionSearchJob != null)
                    {
                        _versionSearchJob.Cancel();
                        _versionSearchJob.Updated -= UpdateCompletions;
                    }
                    _nameSearchJob?.Cancel();
                    _nameSearchJob = null;
                    _versionSearchJob = _searchManager.SearchPackageVersions(name, tfm);
                    _versionSearchJob.Updated += UpdateCompletions;
                    showLoading = _versionSearchJob.RemainingFeeds.Count > 0;
                    break;
            }

            _currentCompletionSet = _currentSession.IsDismissed ? null : _currentSession.CompletionSets.FirstOrDefault(x => x is PackageCompletionSet) as PackageCompletionSet;

            bool newCompletionSet = _currentCompletionSet == null;
            if (newCompletionSet)
            {
                _currentCompletionSet = new PackageCompletionSet("PackageCompletion", "Package Completion", _textBuffer.CurrentSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive));
            }

            if (_nameSearchJob != null)
            {
                ProduceNameCompletionSet();
            }
            else if (_versionSearchJob != null)
            {
                ProduceVersionCompletionSet();
            }

            //If we're not part of an existing session & the results have already been
            //  finalized and those results assert that no packages match, show that
            //  there is no such package/version
            if (!session.IsDismissed && !session.CompletionSets.Any(x => x is PackageCompletionSet))
            {
                if (((_nameSearchJob != null && _nameSearchJob.RemainingFeeds.Count == 0)
                    || (_versionSearchJob != null && _versionSearchJob.RemainingFeeds.Count == 0))
                    && _currentCompletionSet.Completions.Count == 0)
                {
                    _currentCompletionSet.AccessibleCompletions.Add(new Microsoft.VisualStudio.Language.Intellisense.Completion("(No Results)"));
                }

                completionSets.Add(_currentCompletionSet);
            }
        }

        private void ProduceNameCompletionSet()
        {
            List<Microsoft.VisualStudio.Language.Intellisense.Completion> completions = new List<Microsoft.VisualStudio.Language.Intellisense.Completion>();
            Dictionary<string, FeedKind> packageLookup = new Dictionary<string, FeedKind>();

            foreach(Tuple<string, FeedKind> info in _nameSearchJob.Results)
            {
                if(!packageLookup.TryGetValue(info.Item1, out FeedKind existingInfo) || info.Item2 == FeedKind.Local)
                {
                    packageLookup[info.Item1] = info.Item2;
                }
            }

            foreach (KeyValuePair<string, FeedKind> entry in packageLookup.OrderBy(x => x.Key))
            {
                ImageMoniker moniker = KnownMonikers.NuGet;

                switch (entry.Value)
                {
                    case FeedKind.Local:
                        moniker = KnownMonikers.FolderClosed;
                        break;
                        //TODO: Add different icons for MyGet/network/etc
                }

                completions.Add(new PackageCompletion(entry.Key, entry.Key, entry.Key, moniker, entry.Key));
            }

            _currentCompletionSet.AccessibleCompletions.Clear();
            _currentCompletionSet.AccessibleCompletions.AddRange(completions);
        }

        private void ProduceVersionCompletionSet()
        {
            List<Microsoft.VisualStudio.Language.Intellisense.Completion> completions = new List<Microsoft.VisualStudio.Language.Intellisense.Completion>();
            Dictionary<string, FeedKind> iconMap = new Dictionary<string, FeedKind>();

            foreach (Tuple<string, FeedKind> info in _versionSearchJob.Results)
            {
                if (!iconMap.TryGetValue(info.Item1, out FeedKind existing) || existing != FeedKind.Local)
                {
                    iconMap[info.Item1] = info.Item2;
                }
            }

            foreach (KeyValuePair<string, FeedKind> entry in iconMap.OrderByDescending(x => SemanticVersion.Parse(x.Key)))
            {
                ImageMoniker moniker = KnownMonikers.NuGet;

                switch (entry.Value)
                {
                    case FeedKind.Local:
                        moniker = KnownMonikers.FolderClosed;
                        break;
                        //TODO: Add different icons for MyGet/network/etc
                }

                completions.Add(new VersionCompletion(entry.Key, entry.Key, null, moniker, entry.Key));
            }

            _currentCompletionSet.AccessibleCompletions.Clear();
            _currentCompletionSet.AccessibleCompletions.AddRange(completions);
        }

        private async void UpdateCompletions(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                if (_currentCompletionSet == null || _currentSession == null)
                {
                    return;
                }

                string displayText = _currentCompletionSet?.SelectionStatus?.Completion?.DisplayText;

                if (_nameSearchJob != null)
                {
                    ProduceNameCompletionSet();
                }
                else if (_versionSearchJob != null)
                {
                    ProduceVersionCompletionSet();
                }

                if (!_currentSession.IsStarted && _currentCompletionSet.Completions.Count > 0)
                {
                    _isSelfTrigger = true;
                    _currentSession = _completionBroker.CreateCompletionSession(_currentSession.TextView, _currentSession.GetTriggerPoint(_textBuffer), true);
                    _currentSession.Start();
                }

                if (!_currentSession.IsDismissed)
                {
                    _currentSession.Filter();

                    if (displayText != null)
                    {
                        foreach (Microsoft.VisualStudio.Language.Intellisense.Completion completion in _currentSession.SelectedCompletionSet.Completions)
                        {
                            if (completion.DisplayText == displayText)
                            {
                                _currentCompletionSet.SelectionStatus = new CompletionSelectionStatus(completion, true, true);
                                break;
                            }
                        }
                    }

                    //_currentSession.Dismiss();
                    //_currentSession = _completionBroker.TriggerCompletion(_currentSession.TextView);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void Dispose()
        {
        }
    }
}

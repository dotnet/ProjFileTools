using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace ProjectFileTools.Completion
{
    class CompletionController : IOleCommandTarget
    {
        private ICompletionSession _currentSession;

        public CompletionController(IWpfTextView textView, ICompletionBroker broker)
        {
            _currentSession = null;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; private set; }

        public ICompletionBroker Broker { get; private set; }

        public IOleCommandTarget Next { get; set; }

        private static char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            bool handled = false;
            int hresult = VSConstants.S_OK;

            // 1. Pre-process
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                        StartSession();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = Complete(false);
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Complete(true);
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Cancel();
                        break;
                }
            }

            if (!handled)
            {
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            if (ErrorHandler.Succeeded(hresult))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            char ch = GetTypeChar(pvaIn);

                            if (!char.IsControl(ch))
                            {
                                StartSession();
                                Filter();
                            }
                            else if (_currentSession != null)
                            {
                                Filter();
                            }
                            break;
                        case VSConstants.VSStd2KCmdID.BACKSPACE:
                            if (_currentSession == null)
                            {
                                StartSession();
                            }

                            Filter();
                            break;
                    }
                }
            }

            return hresult;
        }

        private void Filter()
        {
            if (_currentSession == null || _currentSession.SelectedCompletionSet == null)
            {
                if (Broker.IsCompletionActive(TextView))
                {
                    _currentSession = Broker.GetSessions(TextView).FirstOrDefault();
                }

                if (_currentSession == null || _currentSession.SelectedCompletionSet == null)
                {
                    return;
                }

                _currentSession.Dismissed += (sender, args) =>
                {
                    if (sender is ICompletionSession session)
                        session.Committed -= HandleCompletionSessionCommit;
                    _currentSession = null;
                };
                _currentSession.Committed += HandleCompletionSessionCommit;
            }

            if (_currentSession != null)
            {
                if (_currentSession.TextView.TextBuffer.Properties.TryGetProperty(typeof(PackageCompletionSource), out ICompletionSource src))
                {
                    src.AugmentCompletionSession(_currentSession, new List<CompletionSet>());
                }

                _currentSession.Filter();
            }
        }

        bool Cancel()
        {
            if (_currentSession == null)
            {
                if (Broker.IsCompletionActive(TextView))
                {
                    _currentSession = Broker.GetSessions(TextView).FirstOrDefault();
                }

                if (_currentSession == null)
                {
                    return false;
                }
            }

            _currentSession.Dismiss();

            return true;
        }

        bool Complete(bool force)
        {
            var currentSession = _currentSession;

            if (currentSession == null)
            {
                if (Broker.IsCompletionActive(TextView))
                {
                    currentSession = Broker.GetSessions(TextView).FirstOrDefault();
                }

                _currentSession = currentSession;

                if (currentSession == null)
                {
                    return false;
                }

                currentSession.Dismissed += (sender, args) =>
                {
                    if (sender is ICompletionSession session)
                        session.Committed -= HandleCompletionSessionCommit;
                    _currentSession = null;
                };
                currentSession.Committed += HandleCompletionSessionCommit;
            }

            if (currentSession.SelectedCompletionSet != null && currentSession.SelectedCompletionSet.SelectionStatus != null && !currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                currentSession.Dismiss();
                _currentSession = null;
                return false;
            }
            else if (currentSession.SelectedCompletionSet != null && currentSession.SelectedCompletionSet.SelectionStatus != null)
            {
                currentSession.Commit();
                _currentSession = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        bool StartSession()
        {
            if (Broker.IsCompletionActive(TextView))
            {
                return false;
            }
            else
            {
                _currentSession = null;
            }

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;

            if (caret.Position == 0)
            {
                return false;
            }

            ITextSnapshot snapshot = caret.Snapshot;
            ICompletionSession currentSession = _currentSession;

            if (!Broker.IsCompletionActive(TextView))
            {
                currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
            }
            else
            {
                currentSession = Broker.GetSessions(TextView)[0];
            }

            _currentSession = currentSession;

            if (currentSession != null)
            {
                currentSession.Dismissed += (sender, args) =>
                {
                    if (sender is ICompletionSession session)
                        session.Committed -= HandleCompletionSessionCommit;
                    _currentSession = null;
                };

                currentSession.Committed += HandleCompletionSessionCommit;

                if (!currentSession.IsStarted)
                {
                    currentSession.Start();
                }
            }

            return true;
        }

        private void HandleCompletionSessionCommit(object sender, EventArgs e)
        {
            CompletionSet completionSet = ((ICompletionSession)sender).SelectedCompletionSet;

            if (completionSet == null)
            {
                return;
            }

            SnapshotSpan span = completionSet.ApplicableTo.GetSpan(completionSet.ApplicableTo.TextBuffer.CurrentSnapshot);
            int caret = span.Span.Start;

            if (PackageCompletionSource.TryHealOrAdvanceAttributeSelection(span.Snapshot, ref caret, out Span targetSpan, out string replacementText, out bool isHealingRequired))
            {
                if (isHealingRequired)
                {
                    ITextSnapshot newSnapshot = span.Snapshot.TextBuffer.Replace(targetSpan, replacementText);
                    TextView.Caret.MoveTo(new SnapshotPoint(newSnapshot, caret));
                }
                else
                {
                    TextView.Caret.MoveTo(new SnapshotPoint(span.Snapshot, caret));
                }

                Broker.TriggerCompletion(TextView);
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}

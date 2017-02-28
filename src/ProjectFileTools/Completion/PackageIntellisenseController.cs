using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace ProjectFileTools.Completion
{

    internal class PackageIntellisenseController : IIntellisenseController
    {
        private ICompletionBroker _completionBroker;
        private IList<ITextBuffer> _subjectBuffers;
        private ITextView _textView;

        public PackageIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers, ICompletionBroker completionBroker)
        {
            _textView = textView;
            _subjectBuffers = subjectBuffers.ToList();
            _completionBroker = completionBroker;

            foreach(ITextBuffer buffer in subjectBuffers)
            {
                ConnectSubjectBuffer(buffer);
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            _subjectBuffers.Add(subjectBuffer);
        }

        public void Detach(ITextView textView)
        {
            if (_textView == textView)
            {
                _textView = null;
            }
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            _subjectBuffers.Remove(subjectBuffer);
        }
    }
}

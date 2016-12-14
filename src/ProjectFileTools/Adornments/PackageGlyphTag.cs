using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace ProjectFileTools
{
    internal abstract class IntraTextAdornmentTagBase : IntraTextAdornmentTag
    {
        private readonly ITextView _textView;

        public IntraTextAdornmentTagBase(PositionAffinity? affinity, ITextView textView) 
            : base(new Border { Padding = new Thickness(0, 0, 2, 0) }, OnRemoved, affinity)
        {
            _textView = textView;
            Wrapper.DataContext = Wrapper;
            try
            {
                Wrapper.Height = textView.LineHeight;
            }
            catch { }
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }

        public void UpdateLayout()
        {
            try
            {
                Wrapper.Height = _textView.LineHeight;
            }
            catch { }

            Wrapper.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        public Border Wrapper => (Border)Adornment;

        private static void OnRemoved(object tag, UIElement element)
        {
            ((IntraTextAdornmentTagBase)((MappingTagSpan<IntraTextAdornmentTag>)tag).Tag).OnRemovedInternal(element);
        }

        protected virtual void OnRemovedInternal(UIElement element)
        {
        }
    }

    internal class PackageGlyphTag : IntraTextAdornmentTagBase
    {
        public Image PackageIcon => (Image)((Border)Adornment).Child;

        public PackageGlyphTag(PositionAffinity? affinity, ITextView textView) 
            : base(affinity, textView)
        {
            Wrapper.Child = new Image();
        }
    }
}

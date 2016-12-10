using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace ProjectFileTools
{

    internal class PackageGlyphTag : IntraTextAdornmentTag
    {
        public Image PackageIcon => (Image)((Border)Adornment).Child;
        public Border Wrapper => (Border)Adornment;

        public PackageGlyphTag(ITextView textView) 
            : base(new Border { Child = new Image() }, OnRemoved, null)
        {
            Wrapper.DataContext = Wrapper;
            Wrapper.Height = textView.LineHeight;
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }

        private static void OnRemoved(object tag, UIElement element)
        {
        }

        public PackageGlyphTag(PositionAffinity? affinity, ITextView textView) 
            : base(new Border { Child = new Image() }, OnRemoved, affinity)
        {
            Wrapper.DataContext = Wrapper;
            Wrapper.Height = textView.LineHeight;
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }

        public PackageGlyphTag(double? topSpace, double? baseline, double? textHeight, double? bottomSpace, PositionAffinity? affinity, IWpfTextView textView) 
            : base(new Border { Child = new Image() }, OnRemoved, topSpace, baseline, textHeight, bottomSpace, affinity)
        {
            Wrapper.DataContext = Wrapper;
            Wrapper.Height = textView.LineHeight;
            Wrapper.SetBinding(FrameworkElement.WidthProperty, "ActualHeight");
        }
    }
}

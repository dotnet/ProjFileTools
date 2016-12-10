//using System;
//using System.Windows.Media;
//using Microsoft.VisualStudio.Imaging;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Text.Formatting;
//using System.Windows.Controls;

//namespace ProjPackageIntellisense
//{
//    internal class PackageIconAdorner
//    {
//        private readonly IAdornmentLayer _layer;
//        private readonly IWpfTextView _textView;

//        public PackageIconAdorner(IWpfTextView textView)
//        {
//            _textView = textView;
//            _textView.Closed += Cleanup;
//            _layer = textView.GetAdornmentLayer("TextAdornment");
//            _textView.LayoutChanged += OnLayoutChanged;
//            ViewState state = new ViewState(textView, textView.ViewportWidth, textView.ViewportHeight);
//            OnLayoutChanged(null, new TextViewLayoutChangedEventArgs(state, state, _textView.TextViewLines, new ITextViewLine[0]));
//        }

//        private void Cleanup(object sender, EventArgs e)
//        {
//            _textView.LayoutChanged -= OnLayoutChanged;
//            _textView.Properties.RemoveProperty("PackageIconAdorner");
//        }

//        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
//        {
//            foreach (ITextViewLine line in e.NewOrReformattedLines)
//            {
//                CreateVisuals(line);
//            }
//        }

//        private void CreateVisuals(ITextViewLine line)
//        {
//            ITextSnapshot snapshot = line.Snapshot;
//            string lineText = snapshot.GetText(line.Extent);
//            int foundAt = 0;
//            foundAt = lineText.IndexOf("Include", foundAt);
//            while (foundAt > -1)
//            {
//                int quote = lineText.IndexOf('"', foundAt);

//                if (quote > -1 && quote < lineText.Length - 1)
//                {
//                    if (PackageCompletionSource.IsInRangeForPackageCompletion(snapshot, quote + 1, out Span s, out string id, out string ver, out string completionType) && completionType == "Name")
//                    {
//                        SnapshotSpan span = new SnapshotSpan(snapshot, Span.FromBounds(s.Start, s.Start));
//                        Geometry geometry = _textView.TextViewLines.GetTextMarkerGeometry(span);
//                        Image image = new Image { Source = WpfUtil.MonikerToBitmap(KnownMonikers.NuGet, 16) };
//                        Canvas.SetLeft(image, geometry.Bounds.Left);
//                        Canvas.SetTop(image, geometry.Bounds.Top);
//                        _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
//                    }
//                }

//                foundAt += "Include".Length;

//                if (foundAt >= lineText.Length)
//                {
//                    foundAt = -1;
//                }
//                else
//                {
//                    foundAt = lineText.IndexOf("Include", foundAt);
//                }
//            } while (foundAt > -1) ;
//        }

//        public static void Attach(IWpfTextView textView)
//        {
//            PackageIconAdorner adorner = new PackageIconAdorner(textView);
//            textView.Properties.AddProperty("PackageIconAdorner", adorner);
//        }
//    }
//}

using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectFileTools
{
    internal static class WpfUtil
    {
        public static BitmapSource MonikerToBitmap(ImageMoniker moniker, int size)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var shell = ServiceUtil.GetService<SVsUIShell, IVsUIShell5>();
            var backgroundColor = shell.GetThemedColorRgba(EnvironmentColors.MainWindowButtonActiveBorderBrushKey);

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags | unchecked((uint)_ImageAttributesFlags.IAF_Background),
                //Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                Dpi = 96,
                LogicalHeight = size,
                LogicalWidth = size,
                Background = backgroundColor,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            var service = ServiceUtil.GetService<SVsImageService, IVsImageService2>();
            IVsUIObject result = service.GetImage(moniker, imageAttributes);
            result.get_Data(out object data);

            return data as BitmapSource;
        }
    }
}

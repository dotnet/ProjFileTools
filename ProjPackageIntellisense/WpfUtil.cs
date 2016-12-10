using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjPackageIntellisense
{
    internal static class WpfUtil
    {
        public static BitmapSource MonikerToBitmap(ImageMoniker moniker, int size)
        {
            var shell = (IVsUIShell5)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));
            //var backgroundColor = VsColors.GetThemedColorRgba(shell, EnvironmentColors.MainWindowButtonActiveBorderBrushKey);

            var imageAttributes = new ImageAttributes
            {
                //Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags | unchecked((uint)_ImageAttributesFlags.IAF_Background),
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                Dpi = 96,
                LogicalHeight = size,
                LogicalWidth = size,
                //Background = backgroundColor,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            var service = (IVsImageService2)Package.GetGlobalService(typeof(SVsImageService));
            IVsUIObject result = service.GetImage(moniker, imageAttributes);
            result.get_Data(out object data);

            return data as BitmapSource;
        }
    }
}

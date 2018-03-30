using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectFileTools
{
    internal static class ServiceUtil
    {
        private static DTE _dte;

        public static DTE DTE => _dte ?? (_dte = GetService<SDTE, DTE>());

        public static TService GetService<TService>() => GetService<TService, TService>();

        public static TInterface GetService<TService, TInterface>()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (TInterface)ServiceProvider.GlobalProvider.GetService(typeof(TService));
        }
    }
}

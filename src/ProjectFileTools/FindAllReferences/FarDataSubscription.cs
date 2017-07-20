using System;
using Microsoft.VisualStudio.Shell.TableManager;

namespace ProjectFileTools.FindAllReferences
{
    /// <summary>
    /// A dummy class for data source subscription
    /// </summary>
    public class FarDataSubscription : IDisposable
    {
        // reserve for future use
        private ITableDataSink _sink;

        public FarDataSubscription(ITableDataSink sink)
        {
            _sink = sink;
        }

        public void Dispose()
        {
        }
    }
}

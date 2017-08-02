using System;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using ProjectFileTools.FindAllReferences;

namespace FarTestProvider
{
    public class FarDataSource : ITableDataSource
    {
        public const string Name = "FAR_TestDataSource";

        public ITableDataSink Sink { get; private set; }
        public ITableEntriesSnapshot[] Snapshots;

        public FarDataSource(int snapshotCapacity)
        {
            this.Snapshots = new FarDataSnapshot[snapshotCapacity];
        }

        public string DisplayName
        {
            get
            {
                return Name;
            }
        }

        public string Identifier
        {
            get
            {
                return Name;
            }
        }

        public string SourceTypeIdentifier
        {
            get
            {
                return StandardTableDataSources2.FindAllReferencesTableDataSource;
            }
        }

        /// <summary>
        /// Called when a <see cref="IWpfTableControl"/> subscribes to the <see cref="ITableDataSource"/>.
        /// </summary>
        /// <param name="sink">A <see cref="ITableDataSink"/> for <see cref="IWpfTableControl"/> and <see cref="ITableDataSource"/> to share data.</param>
        /// <returns>A disposable object representing this subscription. Can be disposed of by the <see cref="IWpfTableControl"/> when unsubscribed.</returns>
        public IDisposable Subscribe(ITableDataSink sink)
        {
            this.Sink = sink;
            return new FarDataSubscription(sink);
        }
    }
}

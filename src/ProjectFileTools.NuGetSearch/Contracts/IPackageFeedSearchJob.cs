using System;
using System.Collections.Generic;

namespace PackageFeedManager
{
    public interface IPackageFeedSearchJob<T>
    {
        IReadOnlyList<string> RemainingFeeds { get; }

        IReadOnlyList<string> SearchingIn { get; }

        IReadOnlyList<T> Results { get; }

        bool IsCancelled { get; }

        event EventHandler Updated;

        void Cancel();
    }
}

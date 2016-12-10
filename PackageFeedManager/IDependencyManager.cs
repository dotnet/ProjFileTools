using System.Collections.Generic;

namespace PackageFeedManager
{

    public interface IDependencyManager
    {
        T GetComponent<T>();

        IReadOnlyList<T> GetComponents<T>();
    }
}

using System.Collections.Generic;

namespace SDG.Unturned;

internal class ServerCurationItemComparer : IComparer<ServerCurationItem>
{
    public virtual int Compare(ServerCurationItem lhs, ServerCurationItem rhs)
    {
        return lhs.SortOrder.CompareTo(rhs.SortOrder);
    }
}

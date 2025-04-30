using System.Runtime.CompilerServices;

namespace C0deGeek.DeepAssert;

/// <summary>
/// Equality comparer for tracking object pairs by reference, to avoid infinite recursion.
/// </summary>
internal class ReferenceTupleEqualityComparer : IEqualityComparer<(object, object)>
{
    public bool Equals((object, object) x, (object, object) y)
        => ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

    public int GetHashCode((object, object) obj)
    {
        unchecked
        {
            var hash1 = RuntimeHelpers.GetHashCode(obj.Item1);
            var hash2 = RuntimeHelpers.GetHashCode(obj.Item2);
            return (hash1 * 397) ^ hash2;
        }
    }
}
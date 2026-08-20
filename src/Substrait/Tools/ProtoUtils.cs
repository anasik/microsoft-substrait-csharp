using Google.Protobuf.Collections;

namespace Substrait.Tools;

/// <summary>
/// Utility methods for protobuf collections.
/// </summary>
public static class ProtoUtils
{
    /// <summary>
    /// Pre-allocates additional capacity and adds a range of values.
    /// </summary>
    /// <typeparam name="T">The repeated field element type.</typeparam>
    /// <param name="fields">The repeated field to populate.</param>
    /// <param name="count">The number of values to add.</param>
    /// <param name="values">The values to add.</param>
    public static void AllocateAndAddRange<T>(this RepeatedField<T> fields, int count, IEnumerable<T> values)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        int requiredCapacity = checked(fields.Count + count);
        if (requiredCapacity > fields.Capacity)
        {
            fields.Capacity = requiredCapacity;
        }

        fields.AddRange(values);
    }
}

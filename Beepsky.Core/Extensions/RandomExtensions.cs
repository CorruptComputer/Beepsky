namespace Beepsky.Core.Extensions;

/// <summary>
///   Extension methods for the <see cref="Random" /> class.
///   For reference: https://github.com/dotnet/runtime/blob/cf8b0d6d0ae0846f9c2557487692c775d6951587/src/libraries/System.Private.CoreLib/src/System/Random.cs#L182-L335
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    ///   Fills the elements of a specified span with items chosen at random from the provided set of choices, ensuring uniqueness.
    /// </summary>
    /// <param name="rdm">The random number generator to use.</param>
    /// <param name="choices">The items to use to populate the span.</param>
    /// <param name="destination">The span to be filled with items.</param>
    /// <typeparam name="T">The type of span.</typeparam>
    /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
    public static void GetUniqueItems<T>(this Random rdm, ReadOnlySpan<T> choices, Span<T> destination)
    {
        if (choices.Length < destination.Length)
        {
            throw new ArgumentException("Not enough unique item choices to fill destination span.", nameof(destination));
        }

        // Create a working copy to shuffle
        T[] working = new T[choices.Length];
        choices.CopyTo(working);

        // Partial Fisher-Yates shuffle: swap destination.Length items to the front
        for (int i = 0; i < destination.Length; i++)
        {
            int j = rdm.Next(i, working.Length);

            // Move selected item to position i
            destination[i] = working[j];

            // Swap to prevent selecting same index again
            (working[i], working[j]) = (working[j], working[i]);
        }
    }

    /// <summary>
    ///   Creates an array populated with items chosen at random from the provided set of choices, ensuring uniqueness.
    /// </summary>
    /// <param name="rdm">The random number generator to use.</param>
    /// <param name="choices">The items to use to populate the array.</param>
    /// <param name="length">The length of array to return.</param>
    /// <typeparam name="T">The type of array.</typeparam>
    /// <returns>An array populated with random items.</returns>
    /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="choices" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="length" /> is not zero or a positive number.
    /// </exception>
    public static T[] GetUniqueItems<T>(this Random rdm, T[] choices, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        return rdm.GetUniqueItems(new ReadOnlySpan<T>(choices), length);
    }

    /// <summary>
    ///   Creates an array populated with items chosen at random from the provided set of choices, ensuring uniqueness.
    /// </summary>
    /// <param name="rdm">The random number generator to use.</param>
    /// <param name="choices">The items to use to populate the array.</param>
    /// <param name="length">The length of array to return.</param>
    /// <typeparam name="T">The type of array.</typeparam>
    /// <returns>An array populated with random items.</returns>
    /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="length" /> is not zero or a positive number.
    /// </exception>
    public static T[] GetUniqueItems<T>(this Random rdm, ReadOnlySpan<T> choices, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        T[] items = new T[length];
        rdm.GetUniqueItems(choices, items.AsSpan());
        return items;
    }
}

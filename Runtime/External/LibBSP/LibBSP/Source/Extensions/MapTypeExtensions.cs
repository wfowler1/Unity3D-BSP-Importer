namespace LibBSP
{

    /// <summary>
    /// Class containing helper methods for the <see cref="MapType"/> enum.
    /// </summary>
    public static class MapTypeExtensions
    {

        /// <summary>
        /// Determines whether this <see cref="MapType"/> is a derivative of <paramref name="other"/>.
        /// </summary>
        /// <param name="type">This <see cref="MapType"/>.</param>
        /// <param name="other">The <see cref="MapType"/> to compare to.</param>
        /// <returns>Whether or not this <see cref="MapType"/> is a derivative of <paramref name="other"/>.</returns>
        public static bool IsSubtypeOf(this MapType type, MapType other)
        {
            return (type & other) == other;
        }

    }
}

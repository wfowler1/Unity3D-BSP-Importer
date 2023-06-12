namespace LibBSP
{

    /// <summary>
    /// Interface for a Lump object.
    /// </summary>
    public interface ILump
    {

        /// <summary>
        /// The <see cref="BSP"/> this <see cref="ILump"/> came from.
        /// </summary>
        BSP Bsp { get; set; }

        /// <summary>
        /// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="ILump"/>.
        /// </summary>
        LumpInfo LumpInfo { get; }

        /// <summary>
        /// Gets the length of this lump in bytes.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets all the data in this lump as a byte array.
        /// </summary>
        /// <param name="lumpOffset">The offset of the beginning of this lump.</param>
        /// <returns>The data.</returns>
        byte[] GetBytes(int lumpOffset);

    }
}

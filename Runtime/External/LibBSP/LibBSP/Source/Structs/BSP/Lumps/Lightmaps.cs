#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;

namespace LibBSP
{
#if UNITY
    using Color = UnityEngine.Color32;
#elif GODOT
    using Color = Godot.Color;
#else
    using Color = System.Drawing.Color;
#endif

    /// <summary>
    /// Holds the visibility data for a BSP.
    /// </summary>
    public class Lightmaps : ILump
    {

        /// <summary>
        /// The <see cref="BSP"/> this <see cref="ILump"/> came from.
        /// </summary>
        public BSP Bsp { get; set; }

        /// <summary>
        /// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="ILump"/>.
        /// </summary>
        public LumpInfo LumpInfo { get; protected set; }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for lightmap info.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets the length of this lump in bytes.
        /// </summary>
        public int Length
        {
            get
            {
                return Data.Length;
            }
        }

        /// <summary>
        /// Parses the passed <c>byte</c> array into a <see cref="Lightmaps"/> object.
        /// </summary>
        /// <param name="data">Array of <c>byte</c>s to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Lightmaps(byte[] data, BSP bsp, LumpInfo lumpInfo = default(LumpInfo))
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Bsp = bsp;
            LumpInfo = lumpInfo;
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type.IsSubtypeOf(MapType.CoD))
            {
                return 1;
            }
            else if (type.IsSubtypeOf(MapType.UberTools))
            {
                return 2;
            }
            else if (type.IsSubtypeOf(MapType.Quake2))
            {
                return 7;
            }
            else if (type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Source))
            {
                return 8;
            }
            else if (type == MapType.Nightfire)
            {
                return 10;
            }
            else if (type.IsSubtypeOf(MapType.Quake3))
            {
                return 14;
            }

            return -1;
        }

        /// <summary>
        /// Gets all the data in this lump as a byte array.
        /// </summary>
        /// <param name="lumpOffset">The offset of the beginning of this lump.</param>
        /// <returns>The data.</returns>
        public byte[] GetBytes(int lumpOffset = 0)
        {
            return Data;
        }

    }
}

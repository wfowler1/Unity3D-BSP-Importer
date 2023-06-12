using System;
using System.Collections.Generic;

namespace LibBSP
{

    /// <summary>
    /// Holds the visibility data for a BSP.
    /// </summary>
    public class Visibility : ILump
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
        /// Array of <c>byte</c>s used as the data source for visibility info.
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
        /// Gets or sets the number of visibility clusters in this <see cref="Visibility"/> lump.
        /// </summary>
        public int NumClusters
        {
            get
            {
                if (Bsp.MapType.IsSubtypeOf(MapType.Quake2)
                    || Bsp.MapType.IsSubtypeOf(MapType.Quake3)
                    || Bsp.MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 0);
                }

                return -1;
            }
            set
            {
                if (Bsp.MapType.IsSubtypeOf(MapType.Quake2)
                    || Bsp.MapType.IsSubtypeOf(MapType.Quake3)
                    || Bsp.MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets the size in bytes of the visibility data for this map's <see cref="Leaf"/> clusters.
        /// </summary>
        public int ClusterSize
        {
            get
            {
                if (Bsp.MapType.IsSubtypeOf(MapType.Quake3))
                {
                    return BitConverter.ToInt32(Data, 4);
                }

                return -1;
            }
            set
            {
                if (Bsp.MapType.IsSubtypeOf(MapType.Quake3))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 4);
                }
            }
        }

        /// <summary>
        /// Parses the passed <c>byte</c> array into a <see cref="Visibility"/> object.
        /// </summary>
        /// <param name="data">Array of <c>byte</c>s to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Visibility(byte[] data, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo))
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
        /// Can <see cref="Leaf"/> <paramref name="leaf"/> see the <see cref="Leaf"/> or cluster at index <paramref name="other"/>.
        /// </summary>
        /// <param name="leaf">The <see cref="Leaf"/>.</param>
        /// <param name="other">The index of the other <see cref="Leaf"/> or cluster to determine visibility for.</param>
        /// <returns>Whether <paramref name="leaf"/> can see the <see cref="Leaf"/> or cluster at index <paramref name="other"/>.</returns>
        public bool CanSee(Leaf leaf, int other)
        {
            int offset = GetOffsetForClusterPVS(leaf.Visibility);
            if (offset < 0)
            {
                offset = leaf.Visibility;
            }

            for (int i = 1; i < leaf.Parent.Bsp.Leaves.Count; ++offset)
            {
                if (Data[offset] == 0 && Bsp.MapType != MapType.Nightfire && !Bsp.MapType.IsSubtypeOf(MapType.Quake))
                {
                    i += 8 * Data[offset + 1];
                    if (i > other)
                    {
                        return false;
                    }
                    offset++;
                }
                else
                {
                    for (int bit = 1; bit != 0; bit = bit * 2, i++)
                    {
                        if (other == i)
                        {
                            return ((Data[offset] & bit) > 0);
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the offset from the beginning of this lump for the given <paramref name="cluster"/>'s PVS data, if applicable.
        /// </summary>
        /// <param name="cluster">The cluster to get the offset to the PVS data for.</param>
        /// <returns>
        /// The offset from the beginning of this lump to the given <paramref name="cluster"/>'s PVS data, or
        /// -1 if this BSP does not use vis clusters.
        /// </returns>
        public int GetOffsetForClusterPVS(int cluster)
        {
            if (Bsp.MapType.IsSubtypeOf(MapType.Quake2)
                || Bsp.MapType.IsSubtypeOf(MapType.Source))
            {
                return BitConverter.ToInt32(Data, 4 + (cluster * 8));
            }

            return -1;
        }

        /// <summary>
        /// Gets the offset from the beginning of this lump for the given <paramref name="cluster"/>'s PAS data, if applicable.
        /// </summary>
        /// <param name="cluster">The cluster to get the offset to the PAS data for.</param>
        /// <returns>
        /// The offset from the beginning of this lump to the given <paramref name="cluster"/>'s PAS data, or
        /// -1 if this BSP does not use vis clusters.
        /// </returns>
        public int GetOffsetForClusterPAS(int cluster)
        {
            if (Bsp.MapType.IsSubtypeOf(MapType.Quake2)
                || Bsp.MapType.IsSubtypeOf(MapType.Source))
            {
                return BitConverter.ToInt32(Data, 8 + (cluster * 8));
            }

            return -1;
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type.IsSubtypeOf(MapType.Quake2))
            {
                return 3;
            }
            else if (type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Source))
            {
                return 4;
            }
            else if (type.IsSubtypeOf(MapType.STEF2))
            {
                return 17;
            }
            else if (type == MapType.MOHAADemo)
            {
                return 16;
            }
            else if (type.IsSubtypeOf(MapType.FAKK2)
                || type.IsSubtypeOf(MapType.MOHAA))
            {
                return 15;
            }
            else if (type == MapType.CoD
                || type == MapType.CoDDemo)
            {
                return 28;
            }
            else if (type == MapType.CoD2)
            {
                return 36;
            }
            else if (type == MapType.Nightfire)
            {
                return 7;
            }
            else if (type.IsSubtypeOf(MapType.Quake3) && type != MapType.CoD4)
            {
                return 16;
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

        /// <summary>
        /// Decompresses the data in the PVS for one <see cref="Leaf"/> cluster.
        /// </summary>
        /// <param name="data">The data to decompress.</param>
        /// <returns>Decompressed data.</returns>
        public byte[] Decompress(byte[] data)
        {
            List<byte> decompressed = new List<byte>();

            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] == 0)
                {
                    i++;
                    for (int j = 0; j < data[i]; ++j)
                    {
                        decompressed.Add(0);
                    }
                }
                else
                {
                    decompressed.Add(data[i]);
                }
            }

            return decompressed.ToArray();
        }

        /// <summary>
        /// Compresses the data in the PVS for one <see cref="Leaf"/> cluster.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>Compressed data.</returns>
        public byte[] Compress(byte[] data)
        {
            byte[] compressed = new byte[data.Length];
            int writeOffset = 0;

            uint zeroCount = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] == 0)
                {
                    ++zeroCount;
                }

                if (data[i] != 0 || i == data.Length - 1)
                {
                    if (zeroCount > 0)
                    {
                        while (zeroCount > byte.MaxValue)
                        {
                            compressed[writeOffset++] = 0;
                            compressed[writeOffset++] = byte.MaxValue;
                            zeroCount -= byte.MaxValue;
                        }
                        compressed[writeOffset++] = 0;
                        compressed[writeOffset++] = (byte)zeroCount;
                        zeroCount = 0;
                    }

                    if (i < data.Length && data[i] != 0)
                    {
                        compressed[writeOffset++] = data[i];
                    }
                }
            }

            byte[] output = new byte[writeOffset];
            Array.Copy(compressed, 0, output, 0, writeOffset);
            return output;
        }

    }
}

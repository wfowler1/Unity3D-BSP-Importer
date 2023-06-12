using System;
using System.Collections.Generic;

namespace LibBSP
{

    /// <summary>
    /// Class for holding and generating data for a header for any <see cref="MapType"/> or <see cref="BSP"/>.
    /// </summary>
    public struct BSPHeader
    {

        /// <summary>
        /// "IBSP" represented as int32.
        /// </summary>
        public const int IBSPHeader = 1347633737;
        /// <summary>
        /// "RBSP" represented as int32.
        /// </summary>
        public const int RBSPHeader = 1347633746;
        /// <summary>
        /// "VBSP" represented as int32.
        /// </summary>
        public const int VBSPHeader = 1347633750;
        /// <summary>
        /// "EALA" represented as int32.
        /// </summary>
        public const int EALAHeader = 1095516485;
        /// <summary>
        /// "2015" represented as int32.
        /// </summary>
        public const int MOHAAHeader = 892416050;
        /// <summary>
        /// "EF2!" represented as int32.
        /// </summary>
        public const int EF2Header = 556942917;
        /// <summary>
        /// "rBSP" represented as int32.
        /// </summary>
        public const int rBSPHeader = 1347633778;
        /// <summary>
        /// "FAKK" represented as int32.
        /// </summary>
        public const int FAKKHeader = 1263223110;

        /// <summary>
        /// The <see cref="BSP"/> this header came from.
        /// </summary>
        public BSP Bsp
        {
            get; private set;
        }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for the header.
        /// </summary>
        public byte[] Data
        {
            get; set;
        }

        public int Length
        {
            get
            {
                if (Data == null)
                {
                    return 0;
                }

                return Data.Length;
            }
        }

        /// <summary>
        /// Gets or sets the revision of the BSP if its type is <see cref="MapType.UberTools"/>.
        /// </summary>
        public int Revision
        {
            get
            {
                if (Bsp.MapType.IsSubtypeOf(MapType.UberTools))
                {
                    return BitConverter.ToInt32(Data, 8);
                }

                return 0;
            }
            set
            {
                if (Bsp.MapType.IsSubtypeOf(MapType.UberTools))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 8);
                }
            }
        }

        /// <summary>
        /// Constructs a <see cref="BSPHeader"/> for <paramref name="bsp"/> using
        /// <paramref name="data"/> as the data source.
        /// </summary>
        /// <param name="bsp">The <see cref="BSP"/> this header is for.</param>
        /// <param name="data">The data for the header.</param>
        public BSPHeader(BSP bsp, byte[] data)
        {
            Bsp = bsp;
            Data = data;

        }

        /// <summary>
        /// Updates header data to reflect the current state of the lumps in <see cref="Bsp"/>.
        /// </summary>
        public BSPHeader Regenerate()
        {
            if (Bsp != null && Bsp.MapType != MapType.Undefined)
            {
                int lumpInfoLength = GetLumpInfoLength(Bsp.MapType);
                int numLumps = BSP.GetNumLumps(Bsp.MapType);
                byte[] magic = GetMagic(Bsp.MapType);
                int revision = Revision;

                if (Bsp.MapType == MapType.CoD4)
                {
                    int numActualLumps = 0;
                    int lumpOffset = magic.Length + 4;

                    Dictionary<int, LumpInfo> lumpInfos = new Dictionary<int, LumpInfo>();
                    for (int i = 0; i < BSP.GetNumLumps(MapType.CoD4); ++i)
                    {
                        LumpInfo lumpInfo = GetLumpInfo(i);

                        int lumpLength;
                        ILump lump = Bsp.GetLoadedLump(lumpInfo.ident);
                        if (lump != null)
                        {
                            lumpLength = lump.Length;
                        }
                        else
                        {
                            // If the lump is not loaded, it has not changed. Use original length.
                            lumpLength = lumpInfo.length;
                        }

                        if (lumpLength > 0)
                        {

                            lumpInfo.offset = lumpOffset;
                            lumpInfo.length = lumpLength;
                            lumpOffset += lumpLength;

                            lumpInfos.Add(i, lumpInfo);

                            ++numActualLumps;
                        }
                    }

                    byte[] newData = new byte[lumpOffset + (lumpInfoLength * numActualLumps)];

                    magic.CopyTo(newData, 0);
                    BitConverter.GetBytes(numActualLumps).CopyTo(newData, 8);
                    int offset = magic.Length + 4;

                    foreach (KeyValuePair<int, LumpInfo> pair in lumpInfos)
                    {
                        BitConverter.GetBytes(pair.Key).CopyTo(newData, offset);
                        BitConverter.GetBytes(offset).CopyTo(newData, offset + 4);

                        offset += 8;
                    }

                    return new BSPHeader(Bsp, newData);
                }
                else
                {
                    int offset;
                    byte[] newData;
                    if (Bsp.MapType.IsSubtypeOf(MapType.UberTools))
                    {
                        offset = magic.Length + 4;
                        newData = new byte[offset + (lumpInfoLength * numLumps)];
                        Revision = revision + 1;
                    }
                    else
                    {
                        offset = magic.Length;
                        newData = new byte[offset + (lumpInfoLength * numLumps)];
                    }

                    magic.CopyTo(newData, 0);
                    int lumpOffset = newData.Length;

                    for (int i = 0; i < numLumps; ++i)
                    {
                        int lumpLength;
                        int lumpVersion;
                        int lumpIdent;

                        ILump lump = Bsp.GetLoadedLump(i);
                        if (lump != null)
                        {
                            lumpLength = lump.Length;
                            lumpVersion = Bsp[i].version;
                            lumpIdent = Bsp[i].ident;
                        }
                        else
                        {
                            // If the lump is not loaded, it has not changed. Use original length.
                            LumpInfo lumpInfo = GetLumpInfo(i);
                            lumpLength = lumpInfo.length;
                            lumpVersion = lumpInfo.version;
                            lumpIdent = lumpInfo.ident;
                        }

                        if (Bsp.MapType == MapType.L4D2 || Bsp.MapType == MapType.Source27)
                        {
                            BitConverter.GetBytes(lumpVersion).CopyTo(newData, offset);
                            if (lumpLength > 0)
                            {
                                BitConverter.GetBytes(lumpOffset).CopyTo(newData, offset + 4);
                            }
                            BitConverter.GetBytes(lumpLength).CopyTo(newData, offset + 8);
                            BitConverter.GetBytes(lumpIdent).CopyTo(newData, offset + 12);
                        }
                        else if (Bsp.MapType.IsSubtypeOf(MapType.Source))
                        {
                            if (lumpLength > 0)
                            {
                                BitConverter.GetBytes(lumpOffset).CopyTo(newData, offset);
                            }
                            BitConverter.GetBytes(lumpLength).CopyTo(newData, offset + 4);
                            BitConverter.GetBytes(lumpVersion).CopyTo(newData, offset + 8);
                            BitConverter.GetBytes(lumpIdent).CopyTo(newData, offset + 12);
                        }
                        else if (Bsp.MapType == MapType.CoD || Bsp.MapType == MapType.CoD2)
                        {
                            BitConverter.GetBytes(lumpLength).CopyTo(newData, offset);
                            if (lumpLength > 0)
                            {
                                BitConverter.GetBytes(lumpOffset).CopyTo(newData, offset + 4);
                            }
                        }
                        else
                        {
                            if (lumpLength > 0)
                            {
                                BitConverter.GetBytes(lumpOffset).CopyTo(newData, offset);
                            }
                            BitConverter.GetBytes(lumpLength).CopyTo(newData, offset + 4);
                        }

                        offset += GetLumpInfoLength(Bsp.MapType);

                        lumpOffset += lumpLength;
                    }

                    return new BSPHeader(Bsp, newData);
                }
            }
            else
            {
                return new BSPHeader(Bsp, new byte[0]);
            }
        }

        /// <summary>
        /// Gets the information for lump "<paramref name="index"/>" for <see cref="Bsp"/>.
        /// </summary>
        /// <param name="index">The numerical index of this lump.</param>
        /// <returns>A <see cref="LumpInfo"/> object containing information about the lump.</returns>
        /// <exception cref="IndexOutOfRangeException">"<paramref name="index"/>" is less than zero, or greater than the number of lumps allowed by "<paramref name="version"/>".</exception>
        public LumpInfo GetLumpInfo(int index)
        {
            if (index < 0 || index >= BSP.GetNumLumps(Bsp.MapType))
            {
                throw new IndexOutOfRangeException();
            }

            int lumpInfoLength = GetLumpInfoLength(Bsp.MapType);

            if (Bsp.MapType.IsSubtypeOf(MapType.Quake)
                || Bsp.MapType == MapType.Nightfire)
            {
                return GetLumpInfoAtOffset(4 + (lumpInfoLength * index));
            }
            else if (Bsp.MapType.IsSubtypeOf(MapType.STEF2)
                || Bsp.MapType.IsSubtypeOf(MapType.MOHAA)
                || Bsp.MapType.IsSubtypeOf(MapType.FAKK2))
            {
                return GetLumpInfoAtOffset(12 + (lumpInfoLength * index));
            }
            else if (Bsp.MapType == MapType.Titanfall)
            {
                LumpInfo lumpFileInfo = Bsp.Reader.GetLumpFileLumpInfo(index);
                if (lumpFileInfo.lumpFile != null)
                {
                    return lumpFileInfo;
                }

                return GetLumpInfoAtOffset(lumpInfoLength * (index + 1));
            }
            else if (Bsp.MapType == MapType.CoD4)
            {
                int numlumps = BitConverter.ToInt32(Data, 8);
                int offset = 12;
                int lumpOffset = offset + (numlumps * 8);
                for (int i = 0; i < numlumps; ++i)
                {
                    int id = BitConverter.ToInt32(Data, offset);
                    int length = BitConverter.ToInt32(Data, offset + 4);
                    if (id == index)
                    {
                        return new LumpInfo()
                        {
                            ident = id,
                            offset = lumpOffset,
                            length = length
                        };
                    }
                    else
                    {
                        lumpOffset += length;
                        while (lumpOffset % 4 != 0)
                        {
                            ++lumpOffset;
                        }
                    }
                    offset += 8;
                }

                return default(LumpInfo);
            }
            else if (Bsp.MapType.IsSubtypeOf(MapType.Source))
            {
                LumpInfo lumpFileInfo = Bsp.Reader.GetLumpFileLumpInfo(index);
                if (lumpFileInfo.lumpFile != null)
                {
                    return lumpFileInfo;
                }

                return GetLumpInfoAtOffset(8 + (lumpInfoLength * index));
            }
            else if (Bsp.MapType.IsSubtypeOf(MapType.Quake2)
                || Bsp.MapType.IsSubtypeOf(MapType.Quake3))
            {
                return GetLumpInfoAtOffset(8 + (lumpInfoLength * index));
            }

            return default(LumpInfo);
        }

        /// <summary>
        /// Gets the lump information at offset "<paramref name="offset"/>" for <see cref="Bsp"/>.
        /// </summary>
        /// <param name="offset">The offset of the lump's information.</param>
        /// <returns>A <see cref="LumpInfo"/> object containing information about the lump.</returns>
        private LumpInfo GetLumpInfoAtOffset(int offset)
        {
            int lumpInfoLength = GetLumpInfoLength(Bsp.MapType);
            if (Data.Length < offset + lumpInfoLength)
            {
                return default(LumpInfo);
            }

            int lumpOffset;
            int lumpLength;
            int lumpVersion = 0;
            int lumpIdent = 0;
            if (Bsp.MapType == MapType.L4D2 || Bsp.MapType == MapType.Source27)
            {
                lumpVersion = BitConverter.ToInt32(Data, offset);
                lumpOffset = BitConverter.ToInt32(Data, offset + 4);
                lumpLength = BitConverter.ToInt32(Data, offset + 8);
                lumpIdent = BitConverter.ToInt32(Data, offset + 12);
            }
            else if (Bsp.MapType.IsSubtypeOf(MapType.Source))
            {
                lumpOffset = BitConverter.ToInt32(Data, offset);
                lumpLength = BitConverter.ToInt32(Data, offset + 4);
                lumpVersion = BitConverter.ToInt32(Data, offset + 8);
                lumpIdent = BitConverter.ToInt32(Data, offset + 12);
            }
            else if (Bsp.MapType == MapType.CoD || Bsp.MapType == MapType.CoD2)
            {
                lumpLength = BitConverter.ToInt32(Data, offset);
                lumpOffset = BitConverter.ToInt32(Data, offset + 4);
            }
            else
            {
                lumpOffset = BitConverter.ToInt32(Data, offset);
                lumpLength = BitConverter.ToInt32(Data, offset + 4);
            }

            /*if (bigEndian) {
				byte[] bytes = BitConverter.GetBytes(lumpLength);
				Array.Reverse(bytes);
				lumpLength = BitConverter.ToInt32(bytes, 0);
				bytes = BitConverter.GetBytes(lumpOffset);
				Array.Reverse(bytes);
				lumpOffset = BitConverter.ToInt32(bytes, 0);
			}*/

            return new LumpInfo()
            {
                offset = lumpOffset,
                length = lumpLength,
                version = lumpVersion,
                ident = lumpIdent
            };
        }

        /// <summary>
        /// Gives the magic header number for a BSP of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of <see cref="BSP"/>.</param>
        /// <returns>The magic header number for a BSP of type <paramref name="type"/>.</returns>
        public static byte[] GetMagic(MapType type)
        {
            switch (type)
            {
                case MapType.Quake:
                {
                    return BitConverter.GetBytes(29);
                }
                case MapType.GoldSrc:
                case MapType.BlueShift:
                {
                    return BitConverter.GetBytes(30);
                }
                case MapType.Quake2:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(38).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Daikatana:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(41).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.SoF:
                case MapType.Quake3:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(46).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.SiN:
                case MapType.Raven:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(RBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(1).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.ET:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(47).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.CoD:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(59).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.CoDDemo:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(58).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.CoD2:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(4).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.CoD4:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(IBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(22).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.STEF2:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(EF2Header).CopyTo(bytes, 0);
                    BitConverter.GetBytes(20).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.STEF2Demo:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(FAKKHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(19).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.MOHAA:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(MOHAAHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(19).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.MOHAADemo:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(MOHAAHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(18).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.MOHAABT:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(EALAHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(21).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.FAKK2:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(FAKKHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(12).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Alice:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(FAKKHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(42).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Nightfire:
                {
                    return BitConverter.GetBytes(42);
                }
                case MapType.Source17:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(17).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source18:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(18).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source19:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(19).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source20:
                case MapType.Vindictus:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(20).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.DMoMaM:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(262164).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source21:
                case MapType.L4D2:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(21).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source22:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(22).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source23:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(23).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Source27:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(VBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(27).CopyTo(bytes, 4);
                    return bytes;
                }
                case MapType.Titanfall:
                {
                    byte[] bytes = new byte[8];
                    BitConverter.GetBytes(rBSPHeader).CopyTo(bytes, 0);
                    BitConverter.GetBytes(29).CopyTo(bytes, 4);
                    return bytes;
                }
            }

            return new byte[0];
        }

        /// <summary>
        /// Gets the length of a lump info for a BSP of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of <see cref="BSP"/>.</param>
        /// <returns>The length of a lump info for a BSP of type <paramref name="type"/>.</returns>
        public static int GetLumpInfoLength(MapType type)
        {
            if (type.IsSubtypeOf(MapType.Source))
            {
                return 16;
            }

            return 8;
        }

    }
}

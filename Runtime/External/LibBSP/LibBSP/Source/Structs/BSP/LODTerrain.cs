using System;
using System.Reflection;

namespace LibBSP
{

    /// <summary>
    /// Holds the data for a terrain object in MoHAA.
    /// </summary>
    public struct LODTerrain : ILumpObject
    {

        /// <summary>
        /// The <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public ILump Parent { get; private set; }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for this <see cref="ILumpObject"/>.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The <see cref="LibBSP.MapType"/> to use to interpret <see cref="Data"/>.
        /// </summary>
        public MapType MapType
        {
            get
            {
                if (Parent == null || Parent.Bsp == null)
                {
                    return MapType.Undefined;
                }
                return Parent.Bsp.MapType;
            }
        }

        /// <summary>
        /// The version number of the <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public int LumpVersion
        {
            get
            {
                if (Parent == null)
                {
                    return 0;
                }
                return Parent.LumpInfo.version;
            }
        }

        /// <summary>
        /// Gets or sets the flags for this terrain.
        /// </summary>
        public byte Flags
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return Data[0];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    Data[0] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the scale of this terrain.
        /// </summary>
        public byte Scale
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return Data[1];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    Data[1] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the lightmap UVs for this terrain.
        /// </summary>
        public byte[] LightmapCoordinates
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return new byte[] { Data[2], Data[3] };
                }

                return null;
            }
            set
            {
                if (value.Length != 2)
                {
                    throw new ArgumentException("LightmapCoordinates array must have 2 elements.");
                }

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    Data[2] = value[0];
                    Data[3] = value[1];
                }
            }
        }

        /// <summary>
        /// Gets or sets the texture UVs for this terrain.
        /// </summary>
        public float[] UVs
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return new float[] {
                        BitConverter.ToSingle(Data, 4),
                        BitConverter.ToSingle(Data, 8),
                        BitConverter.ToSingle(Data, 12),
                        BitConverter.ToSingle(Data, 16),
                        BitConverter.ToSingle(Data, 20),
                        BitConverter.ToSingle(Data, 24),
                        BitConverter.ToSingle(Data, 28),
                        BitConverter.ToSingle(Data, 32),
                    };
                }

                return null;
            }
            set
            {
                if (value.Length != 8)
                {
                    throw new ArgumentException("UVs array must have 8 elements.");
                }

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    int offset = 4;
                    for (int i = 0; i < value.Length; ++i)
                    {
                        BitConverter.GetBytes(value[i]).CopyTo(Data, offset + (i * 4));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the X position of this terrain, on a 64x64 grid.
        /// </summary>
        public sbyte X
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return (sbyte)Data[36];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    Data[36] = (byte)value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Y position of this terrain, on a 64x64 grid.
        /// </summary>
        public sbyte Y
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return (sbyte)Data[37];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    Data[37] = (byte)value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Z position of this terrain.
        /// </summary>
        public short BaseZ
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return BitConverter.ToInt16(Data, 38);
                }

                return 0;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    bytes.CopyTo(Data, 38);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="LibBSP.Texture"/> referenced by this terrain.
        /// </summary>
        public Texture Texture
        {
            get
            {
                return Parent.Bsp.Textures[TextureIndex];
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Texture"/> index for this terrain.
        /// </summary>
        public short TextureIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return BitConverter.ToInt16(Data, 40);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    bytes.CopyTo(Data, 40);
                }
            }
        }

        /// <summary>
        /// Gets or sets the lightmap index for this terrain.
        /// </summary>
        public short Lightmap
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return BitConverter.ToInt16(Data, 42);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    bytes.CopyTo(Data, 42);
                }
            }
        }

        /// <summary>
        /// Gets or sets the vertex flags for each vertex in this terrain.
        /// </summary>
        public ushort[,] VertexFlags
        {
            get
            {
                ushort[,] ret = new ushort[2, 63];
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    for (int i = 0; i < ret.GetLength(0); ++i)
                    {
                        for (int j = 0; j < ret.GetLength(1); ++j)
                        {
                            ret[i, j] = BitConverter.ToUInt16(Data, 52 + (i * 126) + (j * 2));
                        }
                    }
                }

                return ret;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    if (value.GetLength(0) != 2 || value.GetLength(1) != 63)
                    {
                        throw new ArgumentException("VertexFlags array must be size (2, 63) elements.");
                    }
                    for (int i = 0; i < value.GetLength(0); ++i)
                    {
                        for (int j = 0; j < value.GetLength(1); ++j)
                        {
                            BitConverter.GetBytes(value[i, j]).CopyTo(Data, 52 + (i * 126) + (j * 2));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the heightmap for each vertex in this terrain.
        /// </summary>
        public byte[,] Heightmap
        {
            get
            {
                byte[,] ret = new byte[9, 9];
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    for (int i = 0; i < ret.GetLength(0); ++i)
                    {
                        for (int j = 0; j < ret.GetLength(1); ++j)
                        {
                            ret[i, j] = Data[304 + (i * 9) + j];
                        }
                    }
                }

                return ret;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    if (value.GetLength(0) != 9 || value.GetLength(1) != 9)
                    {
                        throw new ArgumentException("Heightmap array must be size (9, 9) elements.");
                    }
                    for (int i = 0; i < value.GetLength(0); ++i)
                    {
                        for (int j = 0; j < value.GetLength(1); ++j)
                        {
                            Data[304 + (i * 9) + j] = value[i, j];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="LODTerrain"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="LODTerrain"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public LODTerrain(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="LODTerrain"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="LODTerrain"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="LODTerrain"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="LODTerrain"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public LODTerrain(LODTerrain source, ILump parent)
        {
            Parent = parent;

            if (parent != null && parent.Bsp != null)
            {
                if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.MapType == parent.Bsp.MapType && source.LumpVersion == parent.LumpInfo.version)
                {
                    Data = new byte[source.Data.Length];
                    Array.Copy(source.Data, Data, source.Data.Length);
                    return;
                }
                else
                {
                    Data = new byte[GetStructLength(parent.Bsp.MapType, parent.LumpInfo.version)];
                }
            }
            else
            {
                if (source.Parent != null && source.Parent.Bsp != null)
                {
                    Data = new byte[GetStructLength(source.Parent.Bsp.MapType, source.Parent.LumpInfo.version)];
                }
                else
                {
                    Data = new byte[GetStructLength(MapType.Undefined, 0)];
                }
            }

            Flags = source.Flags;
            Scale = source.Scale;
            LightmapCoordinates = source.LightmapCoordinates;
            UVs = source.UVs;
            X = source.X;
            Y = source.Y;
            BaseZ = source.BaseZ;
            TextureIndex = source.TextureIndex;
            Lightmap = source.Lightmap;
            VertexFlags = source.VertexFlags;
            Heightmap = source.Heightmap;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{LODTerrain}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{LODTerrain}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<LODTerrain> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<LODTerrain>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
        }

        /// <summary>
        /// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
        /// </summary>
        /// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
        /// <param name="lumpVersion">The version number for the lump.</param>
        /// <returns>The length, in <c>byte</c>s, of this struct.</returns>
        /// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
        public static int GetStructLength(MapType mapType, int lumpVersion = 0)
        {
            if (mapType.IsSubtypeOf(MapType.MOHAA))
            {
                return 388;
            }

            throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type == MapType.MOHAADemo)
            {
                return 23;
            }
            else if (type.IsSubtypeOf(MapType.MOHAA))
            {
                return 22;
            }

            return -1;
        }

    }
}

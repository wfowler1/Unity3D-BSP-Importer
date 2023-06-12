#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Text;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
#elif GODOT
    using Vector2 = Godot.Vector2;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
#else
    using Vector2 = System.Numerics.Vector2;
#endif

    /// <summary>
    /// An all-encompassing class to handle the texture information of any given BSP format.
    /// </summary>
    /// <remarks>
    /// The way texture information is stored varies wildly between versions. As a general
    /// rule, this class only handles the lump containing the string of a texture's name,
    /// and data from within the lump associated with it.
    /// For example, Nightfire's texture lump only contains 64-byte null-padded strings, but
    /// Quake 2's has texture scaling included.
    /// </remarks>
    public struct Texture : ILumpObject
    {

        /// <summary>
        /// Number of MipMap levels used by Quake/GoldSrc engines.
        /// </summary>
        public const int NumMipmaps = 4;
        /// <summary>
        /// Index of the full mipmap.
        /// </summary>
        public const int FullMipmap = 0;
        /// <summary>
        /// Index of the half mipmap.
        /// </summary>
        public const int HalfMipmap = 1;
        /// <summary>
        /// Index of the quarter mipmap.
        /// </summary>
        public const int QuarterMipmap = 2;
        /// <summary>
        /// Index of the eighth mipmap.
        /// </summary>
        public const int EighthMipmap = 3;

        /// <summary>
        /// The <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public ILump Parent { get; private set; }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for this <see cref="ILumpObject"/>.
        /// </summary>
        public byte[] Data { get; private set; }

        private byte[][] _mipmaps;
        /// <summary>
        /// Mipmap image data for this <see cref="Texture"/>.
        /// </summary>
        public byte[][] Mipmaps
        {
            get
            {
                if (_mipmaps == null && MipmapFullOffset >= 0)
                {
                    _mipmaps = new byte[NumMipmaps][];
                }
                return _mipmaps;
            }
        }

        private byte[] _palette;
        /// <summary>
        /// Mipmap image palette for this <see cref="Texture"/>, in GBR24(?).
        /// </summary>
        public byte[] Palette
        {
            get
            {
                return _palette;
            }
        }

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
        /// Gets or sets the name of this <see cref="Texture"/>.
        /// </summary>
        public string Name
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return Data.ToNullTerminatedString(0, 16);
                }
                else if (MapType == MapType.SiN)
                {
                    return Data.ToNullTerminatedString(36, 64);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    return Data.ToNullTerminatedString(40, 32);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire)
                {
                    return Data.ToNullTerminatedString(0, 64);
                }
                else if (MapType.IsSubtypeOf(MapType.Source) || MapType == MapType.Titanfall)
                {
                    return Data.ToRawString();
                }

                return null;
            }
            set
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        Data[i] = 0;
                    }
                    Array.Copy(bytes, 0, Data, 0, Math.Min(bytes.Length, 15));
                }
                else if (MapType == MapType.SiN)
                {
                    for (int i = 0; i < 64; ++i)
                    {
                        Data[i + 36] = 0;
                    }
                    Array.Copy(bytes, 0, Data, 36, Math.Min(bytes.Length, 63));
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    for (int i = 0; i < 32; ++i)
                    {
                        Data[i + 40] = 0;
                    }
                    Array.Copy(bytes, 0, Data, 40, Math.Min(bytes.Length, 31));
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire)
                {
                    for (int i = 0; i < 64; ++i)
                    {
                        Data[i] = 0;
                    }
                    Array.Copy(bytes, 0, Data, 0, Math.Min(bytes.Length, 63));
                }
                else if (MapType.IsSubtypeOf(MapType.Source) || MapType == MapType.Titanfall)
                {
                    Data = bytes;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the mask used on this <see cref="Texture"/>.
        /// </summary>
        public string Mask
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return Data.ToNullTerminatedString(76, 64);
                }

                return null;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    for (int i = 0; i < 64; ++i)
                    {
                        Data[i + 76] = 0;
                    }
                    byte[] strBytes = Encoding.ASCII.GetBytes(value);
                    Array.Copy(strBytes, 0, Data, 76, Math.Min(strBytes.Length, 63));
                }
            }
        }

        /// <summary>
        /// Gets or sets the flags on this <see cref="Texture"/>.
        /// </summary>
        public int Flags
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    return BitConverter.ToInt32(Data, 32);
                }
                if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    return BitConverter.ToInt32(Data, 64);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    bytes.CopyTo(Data, 32);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    bytes.CopyTo(Data, 64);
                }
            }
        }

        /// <summary>
        /// Gets or sets the contents flags used by this <see cref="Texture"/>.
        /// </summary>
        public int Contents
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    return BitConverter.ToInt32(Data, 68);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    bytes.CopyTo(Data, 68);
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="LibBSP.TextureInfo"/> in this <see cref="Texture"/>.
        /// </summary>
        public TextureInfo TextureInfo
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    return new TextureInfo(Vector3Extensions.ToVector3(Data),
                                           Vector3Extensions.ToVector3(Data, 16),
                                           new Vector2(BitConverter.ToSingle(Data, 12), BitConverter.ToSingle(Data, 28)),
                                           new Vector2(1, 1),
                                           -1, -1, 0);
                }

                return new TextureInfo();
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    value.UAxis.GetBytes().CopyTo(Data, 0);
                    value.VAxis.GetBytes().CopyTo(Data, 16);
                    BitConverter.GetBytes(value.Translation.X()).CopyTo(Data, 12);
                    BitConverter.GetBytes(value.Translation.Y()).CopyTo(Data, 28);
                }
            }
        }

        /// <summary>
        /// Gets or sets the dimensions of this <see cref="Texture"/>.
        /// </summary>
        public Vector2 Dimensions
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return new Vector2(BitConverter.ToUInt32(Data, 16), BitConverter.ToUInt32(Data, 20));
                }

                return new Vector2(0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    BitConverter.GetBytes((int)value.X()).CopyTo(Data, 16);
                    BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 20);
                }
            }
        }

        /// <summary>
        /// Gets the offset to the full mipmap of this <see cref="Texture"/>.
        /// </summary>
        public int MipmapFullOffset
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 24);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 24);
                }
            }
        }

        /// <summary>
        /// Gets the offset to the half mipmap of this <see cref="Texture"/>.
        /// </summary>
        public int MipmapHalfOffset
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 28);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 28);
                }
            }
        }

        /// <summary>
        /// Gets the offset to the quarter mipmap of this <see cref="Texture"/>.
        /// </summary>
        public int MipmapQuarterOffset
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 32);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Gets the number of pixels in the <see cref="Palette"/>.
        /// </summary>
        public int PaletteSize
        {
            get
            {
                return Palette.Length / 3;
            }
        }

        /// <summary>
        /// Gets the offset to the eighth mipmap of this <see cref="Texture"/>.
        /// </summary>
        public int MipmapEighthOffset
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 36);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 36);
                }
            }
        }

        /// <summary>
        /// Gets the miscellaneous value for this <see cref="Texture"/>.
        /// </summary>
        public int Value
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake2) && MapType != MapType.SiN)
                {
                    return BitConverter.ToInt32(Data, 36);
                }

                return 0;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake2) && MapType != MapType.SiN)
                {
                    bytes.CopyTo(Data, 36);
                }
            }
        }

        /// <summary>
        /// Gets or sets the next frame's <see cref="Texture"/> if this one is animated.
        /// </summary>
        public int Next
        {
            get
            {
                if (MapType == MapType.SiN)
                {
                    return BitConverter.ToInt32(Data, 100);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    return BitConverter.ToInt32(Data, 72);
                }

                return 0;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.SiN)
                {
                    bytes.CopyTo(Data, 100);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    bytes.CopyTo(Data, 72);
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of curve subdivisions for this <see cref="Texture"/>.
        /// </summary>
        public int Subdivisions
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.UberTools))
                {
                    return BitConverter.ToInt32(Data, 72);
                }

                return 16;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.UberTools))
                {
                    bytes.CopyTo(Data, 72);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Texture"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Texture"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Texture(byte[] data, ILump parent) : this(data, parent, null, null) { }

        /// <summary>
        /// Creates a new <see cref="Texture"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Texture"/> came from.</param>
        /// <param name="mipmaps">Data for the mipmap levels.</param>
        /// <param name="palette">Data for the mipmap palette.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Texture(byte[] data, ILump parent, byte[][] mipmaps, byte[] palette)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
            _mipmaps = mipmaps;
            _palette = palette;
        }

        /// <summary>
        /// Creates a new <see cref="Texture"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Texture"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Texture"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Texture"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Texture(Texture source, ILump parent)
        {
            Parent = parent;

            if (parent != null && parent.Bsp != null)
            {
                if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.MapType == parent.Bsp.MapType && source.LumpVersion == parent.LumpInfo.version)
                {
                    Data = new byte[source.Data.Length];
                    Array.Copy(source.Data, Data, source.Data.Length);
                    _mipmaps = source.Mipmaps;
                    _palette = source.Palette;
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

            _mipmaps = source.Mipmaps;
            _palette = source.Palette;
            Name = source.Name;
            Mask = source.Mask;
            Flags = source.Flags;
            Contents = source.Contents;
            TextureInfo = source.TextureInfo;
            Dimensions = source.Dimensions;
            MipmapFullOffset = source.MipmapFullOffset;
            MipmapHalfOffset = source.MipmapHalfOffset;
            MipmapQuarterOffset = source.MipmapQuarterOffset;
            MipmapEighthOffset = source.MipmapEighthOffset;
            Value = source.Value;
            Next = source.Next;
            Subdivisions = source.Subdivisions;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Textures"/> object.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Textures"/> object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Textures LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Textures(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
        }

        /// <summary>
        /// Depending on format, this is a variable length structure. Return -1. The <see cref="Textures"/> class will handle object creation.
        /// </summary>
        /// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
        /// <param name="lumpVersion">The version number for the lump.</param>
        /// <returns>-1</returns>
        public static int GetStructLength(MapType type, int version)
        {
            return -1;
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type.IsSubtypeOf(MapType.UberTools)
                || type.IsSubtypeOf(MapType.CoD))
            {
                return 0;
            }
            else if (type.IsSubtypeOf(MapType.Quake)
                || type == MapType.Nightfire)
            {
                return 2;
            }
            else if (type.IsSubtypeOf(MapType.Quake2))
            {
                return 5;
            }
            else if (type.IsSubtypeOf(MapType.Quake3))
            {
                return 1;
            }
            else if (type.IsSubtypeOf(MapType.Source)
                || type == MapType.Titanfall)
            {
                return 43;
            }

            return -1;
        }

        /// <summary>
        /// Gets the index for the materials lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForMaterialLump(MapType type)
        {
            if (type == MapType.Nightfire)
            {
                return 3;
            }

            return -1;
        }

        /// <summary>
        /// Returns a texture <see cref="name"/> with back slashes converted to forward slashes,
        /// Source cubemap names fixed, etc.
        /// </summary>
        /// <param name="name">The name of the texture to process.</param>
        /// <param name="mapType">The <see cref="MapType"/> of the BSP this texture name is from.</param>
        /// <returns>A sanitized version of the passed <paramref name="name"/>.</returns>
        public static string SanitizeName(string name, MapType mapType)
        {
            string sanitized = name.Replace('\\', '/');
            sanitized = sanitized.Replace('*', '#');

            if (mapType.IsSubtypeOf(MapType.Source)
                || mapType == MapType.Titanfall)
            {
                if (sanitized.Length >= 5 && sanitized.Substring(0, 5).Equals("maps/", StringComparison.InvariantCultureIgnoreCase))
                {
                    sanitized = sanitized.Substring(5);
                    for (int i = 0; i < sanitized.Length; ++i)
                    {
                        if (sanitized[i] == '/')
                        {
                            sanitized = sanitized.Substring(i + 1);
                            break;
                        }
                    }
                }

                // Parse cubemap textures
                // TODO: Use regex? .{1,}(_-?[0-9]{1,}){3}$
                int numUnderscores = 0;
                bool validnumber = false;
                for (int i = sanitized.Length - 1; i > 0; --i)
                {
                    if (sanitized[i] <= '9' && sanitized[i] >= '0')
                    {
                        // Current is a number, this may be a cubemap reference
                        validnumber = true;
                    }
                    else
                    {
                        if (sanitized[i] == '-')
                        {
                            // Current is a minus sign (-).
                            if (!validnumber)
                            {
                                break; // Make sure there's a number to add the minus sign to. If not, kill the loop.
                            }
                        }
                        else
                        {
                            if (sanitized[i] == '_')
                            {
                                // Current is an underscore (_)
                                if (validnumber)
                                {
                                    // Make sure there is a number in the current string
                                    ++numUnderscores; // before moving on to the next one.
                                    if (numUnderscores == 3)
                                    {
                                        // If we've got all our numbers
                                        sanitized = sanitized.Substring(0, i); // Cut the texture string
                                    }
                                    validnumber = false;
                                }
                                else
                                {
                                    // No number after the underscore
                                    break;
                                }
                            }
                            else
                            {
                                // Not an acceptable character
                                break;
                            }
                        }
                    }
                }
            }

            return sanitized;
        }

    }
}
